using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using Random = UnityEngine.Random;

public class UpgradeCardSelect : MonoBehaviour
{
   public GameObject[] upgradeCards;

   private int index;
   private int length;
   
   private GameObject currentCard;
   private Vector3 currentCardScale;
   private GameObject previousCard;
   private Vector3 previousCardScale;
   

   void Start()
   {
      length = upgradeCards.Length;
      index = 0;
      
      SelectCard(index);
   }

   void Update()
   {
      if (Input.GetKeyDown(KeyCode.A))
      {
         index--;
         if (index < 0) index = length - 1;
         SelectCard(index);
         
         Debug.Log(index);
      }

      if (Input.GetKeyDown(KeyCode.D))
      {
         index++;
         if (index >= length) index = 0;
         SelectCard(index);
         
         Debug.Log(index);
      }
   }

   public void SelectCard(int idx)
   {
      previousCard = currentCard;
      previousCardScale = currentCardScale;
      currentCard = upgradeCards[idx];
      currentCardScale = currentCard.transform.localScale;
      
      if (previousCard is not null) StartCoroutine(ChangeScaleCard(previousCard, true));
      if (currentCard is not null) StartCoroutine(ChangeScaleCard(currentCard, false));
      
      if (previousCard is not null) previousCard.transform.localScale = currentCardScale;
      if (currentCard is not null) currentCard.transform.localScale = previousCardScale;
   }

   IEnumerator ChangeScaleCard(GameObject card, bool isShrinking)
   {
      float t = 0f;
   
      Vector3 startScale = card.transform.localScale;
      Vector3 endScale = (isShrinking) ? card.transform.localScale / 1.5f : card.transform.localScale * 1.5f;
      
      while (t <= 1f)
      {
         t += Time.deltaTime / 0.3f;
         
         if (card != null)
         {
            card.transform.localScale = Vector3.Lerp(startScale, endScale, t / 0.3f);
         }
         else
         {
            break;
         }
         
         yield return null;
      }
   }
}