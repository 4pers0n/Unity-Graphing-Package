using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace EvanZ.Tools
{
    public class Window_Graph : MonoBehaviour
    {
        [SerializeField] private RectTransform _graphContainer;
        [SerializeField] private Sprite _circleSprite;

        private void Awake()
        {
            List<int> valueList = new List<int>() { 5, 23, 3, 241, 2, 21, 14, 56, 102, 189 };
            ShowGraph(valueList);
        }

        private GameObject CreateCircle(Vector2 anchoredPosition)
        {
            GameObject gameObject = new GameObject("circle", typeof(Image));
            gameObject.transform.SetParent(_graphContainer, false);
            gameObject.GetComponent<Image>().sprite = _circleSprite;
            RectTransform rectTransform = gameObject.GetComponent<RectTransform>();
            rectTransform.anchoredPosition = anchoredPosition;
            rectTransform.sizeDelta = new Vector2(11, 11);
            rectTransform.anchorMin = new Vector2(0, 0);
            rectTransform.anchorMax = new Vector2(0, 0);
            return gameObject;
        }

        private void ShowGraph(List<int> valueList)
        {
            float graphHeight = _graphContainer.sizeDelta.y;
            float yMaximum = 400;
            float xSize = 50f;
            GameObject lastCircleGameObject = null;
            for (int i = 0; i < valueList.Count; i++)
            {
                float xPosition = i * xSize;
                float yPosition = valueList[i] * (yMaximum / graphHeight);
                GameObject circleGameObject = CreateCircle(new Vector2(xPosition, yPosition));
                if (lastCircleGameObject != null)
                {
                    CreateDotConnection(lastCircleGameObject.GetComponent<RectTransform>().anchoredPosition,
                        circleGameObject.GetComponent<RectTransform>().anchoredPosition);
                }
                lastCircleGameObject = circleGameObject;
            }
        }

        private void CreateDotConnection(Vector2 dotPositionA, Vector2 dotPositionB)
        {
            GameObject gameObject = new GameObject("dotConnection", typeof(Image));
            gameObject.transform.SetParent(_graphContainer);
            gameObject.transform.GetComponent<Image>().color = new Color(1, 1, 1, 0.5f);
            RectTransform rectTransform = gameObject.GetComponent<RectTransform>();
            Vector2 dir = (dotPositionB - dotPositionA).normalized;
            float distance = Vector2.Distance(dotPositionA, dotPositionB);
            rectTransform.anchorMin = new Vector2(0, 0);
            rectTransform.anchorMax = new Vector2(0, 0);
            rectTransform.sizeDelta = new Vector2(distance, 3f);
            rectTransform.anchoredPosition = dotPositionA + dir * distance * 0.5f;
            rectTransform.localEulerAngles = new Vector3(0, 0, Utils.GetAngleFromVectorFloat(dir));
        }
    }
}