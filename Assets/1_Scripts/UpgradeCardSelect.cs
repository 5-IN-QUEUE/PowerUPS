using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using Random = UnityEngine.Random;

[System.Serializable]
public struct CardModelInfo
{
   public GameObject cardModel;
   [Tooltip("체크 시 모델은 위에서 아래로 떨어짐")]
   public bool isUpsideDown;
   public Vector3 offset;
}

[System.Serializable]
public struct CardModel
{
   public CardModelInfo[] cardModels;
}

public class UpgradeCardSelect : MonoBehaviour
{
   public GameObject[] upgradeCards;
   public CardModel[] selectModels;

   private List<GameObject> spawnedModels;
   private Coroutine moveCoroutine;

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
      spawnedModels = new List<GameObject>();
      
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
      if (moveCoroutine != null)
      {
         StopCoroutine(moveCoroutine);
      }
      
      if (spawnedModels != null)
      {
         foreach (GameObject spawnedModel in spawnedModels)
         {
            if (spawnedModel != null)
            {
               Destroy(spawnedModel);
            }
         }
         spawnedModels.Clear();
      }
      
      CardModelInfo[] models = selectModels[idx].cardModels;
      
      int randomIdx = Random.Range(0, models.Length);
      GameObject cardModel = models[randomIdx].cardModel;
      bool isUpsideDown = models[randomIdx].isUpsideDown;
      Vector3 offset = models[randomIdx].offset;

      previousCard = currentCard;
      previousCardScale = currentCardScale;
      currentCard = upgradeCards[idx];
      currentCardScale = currentCard.transform.localScale;
      
      if (previousCard is not null) StartCoroutine(ChangeScaleCard(previousCard, true));
      if (currentCard is not null) StartCoroutine(ChangeScaleCard(currentCard, false));
      
      if (previousCard is not null) previousCard.transform.localScale = currentCardScale;
      if (currentCard is not null) currentCard.transform.localScale = previousCardScale;
      
      Vector3 spawnPos = currentCard.transform.position + Vector3.forward + offset;
      spawnPos += (isUpsideDown) ? transform.up * 30f : Vector3.up * -30f;
      
      GameObject model = Instantiate(cardModel, spawnPos, cardModel.transform.rotation);
      
      spawnedModels.Add(model);
      moveCoroutine = StartCoroutine(MoveModel(model, isUpsideDown));
   }

   IEnumerator MoveModel(GameObject model, bool isUpsideDown)
   {
      float t = 0f;
   
      Vector3 startPos = model.transform.position;
      Vector3 endPos = (isUpsideDown) ? startPos + transform.up * -19.3f : startPos + transform.up * 19.3f;
      
      while (t <= 1f)
      {
         t += Time.deltaTime;
         
         if (model != null)
         {
            model.transform.position = Vector3.Lerp(startPos, endPos, t);
         }
         
         yield return null;
      }
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