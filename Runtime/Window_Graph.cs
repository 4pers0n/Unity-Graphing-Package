using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

namespace EvanZ.Tools
{
    public class Window_Graph : MonoBehaviour
    {
        [SerializeField] private RectTransform _graphContainer;
        [SerializeField] private Sprite _dotSprite;
        [SerializeField] private GameObject _labelTemplateX;
        [SerializeField] private GameObject _labelTemplateY;
        [SerializeField] private GameObject _dashTemplateX;
        [SerializeField] private GameObject _dashTemplateY;

        private List<GameObject> _gameObjectsList;

        // Cached values
        private List<int> _valueList;
        private IGraphVisual _graphVisual;
        private int _maxVisibleValueAmount = -1;
        private Func<int, string> _getAxisLabelX = null;
        private Func<float, string> _getAxisLabelY = null;

        private BarChartVisual barChartVisual;
        private LineGraphVisual lineGraphVisual;

        private void Awake()
        {
            _gameObjectsList = new();

            _valueList = new List<int>() { 5, 23, 12, 4, 45, 80, 105, 203 };

            lineGraphVisual = new(_graphContainer, _dotSprite, Color.green, Color.blue);
            barChartVisual = new(_graphContainer, Color.green, .9f);

            ShowGraph(_valueList, lineGraphVisual, -1);
        }

        public void SetGraphVisualToBar()
        {
            ShowGraph(_valueList, barChartVisual, _maxVisibleValueAmount, _getAxisLabelX, _getAxisLabelY);
        }
        public void SetGraphVisualToLine()
        {
            ShowGraph(_valueList, lineGraphVisual, _maxVisibleValueAmount, _getAxisLabelX, _getAxisLabelY);
        }
        public void IncreaseVisibleAmount()
        {
            ShowGraph(_valueList, _graphVisual, _maxVisibleValueAmount + 1, _getAxisLabelX, _getAxisLabelY);
        }
        public void DecreaseVisibleAmount()
        {
            ShowGraph(_valueList, _graphVisual, _maxVisibleValueAmount - 1, _getAxisLabelX, _getAxisLabelY);
        }

        private void ShowGraph(List<int> valueList, IGraphVisual graphVisual,
            int maxVisibleValueAmount = -1, Func<int, string> getAxisLabelX = null, Func<float, string> getAxisLabelY = null)
        {
            _valueList = valueList;
            _graphVisual = graphVisual;

            if (getAxisLabelX == null)
            {
                getAxisLabelX = (int _i) => { return _i.ToString(); };
            }
            if (getAxisLabelY == null)
            {
                getAxisLabelY = (float _f) => { return Mathf.RoundToInt(_f).ToString(); };
            }
            _getAxisLabelX = getAxisLabelX;
            _getAxisLabelY = getAxisLabelY;

            if (maxVisibleValueAmount <= 0 || maxVisibleValueAmount > valueList.Count)
            {
                maxVisibleValueAmount = valueList.Count;
            }
            _maxVisibleValueAmount = maxVisibleValueAmount;


            foreach (GameObject gameObject in _gameObjectsList)
            {
                Destroy(gameObject);
            }
            _gameObjectsList.Clear();

            float graphWidth = _graphContainer.sizeDelta.x;
            float graphHeight = _graphContainer.sizeDelta.y;

            float yMaximum = _valueList[0];
            float yMinimum = _valueList[0];
            for (int i = Mathf.Max(_valueList.Count - _maxVisibleValueAmount, 0); i < _valueList.Count; i++)
            {
                int value = _valueList[i];
                if (value > yMaximum)
                    yMaximum = value;
                if (value < yMinimum)
                    yMinimum = value;
            }
            yMaximum = yMaximum + (yMaximum - yMinimum) * 0.2f;
            yMinimum = yMinimum - (yMaximum - yMinimum) * 0.2f;
            if (yMaximum == yMinimum)
            {
                yMaximum *= 1.2f;
                yMinimum /= 1.2f;
            }

            float xSize = graphWidth / (_maxVisibleValueAmount + 1);
            int xIndex = 0;

            if (_graphVisual is LineGraphVisual visual)
                visual.ResetLastDotGameObject();

            for (int i = Mathf.Max(_valueList.Count - _maxVisibleValueAmount, 0); i < _valueList.Count; i++)
            {
                float xPosition = xSize + xIndex * xSize;
                float yPosition = (_valueList[i] - yMinimum) / (yMaximum - yMinimum) * graphHeight;

                _gameObjectsList.AddRange(_graphVisual.AddGraphVisual(new Vector2(xPosition, yPosition), xSize));

                RectTransform labelX = Instantiate(_labelTemplateX).GetComponent<RectTransform>();
                labelX.SetParent(_graphContainer);
                labelX.anchoredPosition = new Vector2(xPosition, 0f);
                labelX.GetComponent<TMP_Text>().text = _getAxisLabelX(i);
                _gameObjectsList.Add(labelX.gameObject);

                RectTransform dashX = Instantiate(_dashTemplateX).GetComponent<RectTransform>();
                dashX.SetParent(_graphContainer);
                dashX.anchoredPosition = new Vector2(xPosition, -20f);
                _gameObjectsList.Add(dashX.gameObject);

                xIndex++;
            }

            int separatorCount = 10;
            for (int i = 0; i <= separatorCount; i++)
            {
                RectTransform labelY = Instantiate(_labelTemplateY).GetComponent<RectTransform>();
                labelY.SetParent(_graphContainer);
                float normalizedValue = i * 1f / separatorCount;
                labelY.anchoredPosition = new Vector2(-7f, normalizedValue * graphHeight);
                labelY.GetComponent<TMP_Text>().text = _getAxisLabelY(yMinimum + normalizedValue * (yMaximum - yMinimum));
                _gameObjectsList.Add(labelY.gameObject);


                RectTransform dashY = Instantiate(_dashTemplateY).GetComponent<RectTransform>();
                dashY.SetParent(_graphContainer);
                dashY.anchoredPosition = new Vector2(-4f, normalizedValue * graphHeight);
                _gameObjectsList.Add(dashY.gameObject);
            }
        }

        private interface IGraphVisual
        {
            List<GameObject> AddGraphVisual(Vector2 graphPosition, float graphPositionWidth);
        }

        private class BarChartVisual : IGraphVisual
        {
            private readonly RectTransform _graphContainer;
            private readonly Color _barColor;
            private readonly float _barWidthMultiplier;

            public BarChartVisual(RectTransform graphContainer, Color barColor, float barWidthMultiplier)
            {
                _graphContainer = graphContainer;
                _barColor = barColor;
                _barWidthMultiplier = barWidthMultiplier;
            }

            public List<GameObject> AddGraphVisual(Vector2 graphPosition, float graphPositionWidth)
            {
                GameObject barGameObject = CreateBar(graphPosition, graphPositionWidth * _barWidthMultiplier);
                return new List<GameObject>() { barGameObject };
            }

            private GameObject CreateBar(Vector2 graphPosition, float barWidth)
            {
                GameObject gameObject = new GameObject("bar", typeof(Image));
                gameObject.transform.SetParent(_graphContainer, false);
                gameObject.GetComponent<Image>().color = _barColor;
                RectTransform rectTransform = gameObject.GetComponent<RectTransform>();
                rectTransform.anchoredPosition = new Vector2(graphPosition.x, 0f);
                rectTransform.sizeDelta = new Vector2(barWidth, graphPosition.y);
                rectTransform.anchorMin = new Vector2(0, 0);
                rectTransform.anchorMax = new Vector2(0, 0);
                rectTransform.pivot = new Vector2(0.5f, 0f);
                return gameObject;
            }
        }

        private class LineGraphVisual : IGraphVisual
        {
            private RectTransform _graphContainer;
            private Sprite _dotSprite;
            private GameObject _lastDotGameObject;
            private Color _dotColor;
            private Color _dotConnectionColor;

            public LineGraphVisual(RectTransform graphContainer, Sprite dotSprite, Color dotColor, Color dotConnectionColor)
            {
                _graphContainer = graphContainer;
                _dotSprite = dotSprite;
                _lastDotGameObject = null;
                _dotColor = dotColor;
                _dotConnectionColor = dotConnectionColor;
            }

            public void ResetLastDotGameObject() => _lastDotGameObject = null;

            public List<GameObject> AddGraphVisual(Vector2 graphPosition, float graphPositionWidth)
            {
                List<GameObject> gameObjectsList = new();
                GameObject dotGameObject = CreateDot(graphPosition);
                gameObjectsList.Add(dotGameObject);
                if (_lastDotGameObject != null)
                {
                    GameObject dotConnectionGameObject = CreateDotConnection(_lastDotGameObject.GetComponent<RectTransform>().anchoredPosition,
                        dotGameObject.GetComponent<RectTransform>().anchoredPosition);
                    gameObjectsList.Add(dotConnectionGameObject);
                }
                _lastDotGameObject = dotGameObject;

                return gameObjectsList;
            }

            private GameObject CreateDot(Vector2 anchoredPosition)
            {
                GameObject gameObject = new GameObject("dot", typeof(Image));
                gameObject.transform.SetParent(_graphContainer, false);
                gameObject.GetComponent<Image>().sprite = _dotSprite;
                gameObject.GetComponent<Image>().color = _dotColor;
                RectTransform rectTransform = gameObject.GetComponent<RectTransform>();
                rectTransform.anchoredPosition = anchoredPosition;
                rectTransform.sizeDelta = new Vector2(11, 11);
                rectTransform.anchorMin = new Vector2(0, 0);
                rectTransform.anchorMax = new Vector2(0, 0);
                return gameObject;
            }

            private GameObject CreateDotConnection(Vector2 dotPositionA, Vector2 dotPositionB)
            {
                GameObject gameObject = new GameObject("dotConnection", typeof(Image));
                gameObject.transform.SetParent(_graphContainer);
                gameObject.GetComponent<Image>().color = _dotConnectionColor;
                RectTransform rectTransform = gameObject.GetComponent<RectTransform>();
                Vector2 dir = (dotPositionB - dotPositionA).normalized;
                float distance = Vector2.Distance(dotPositionA, dotPositionB);
                rectTransform.anchorMin = new Vector2(0, 0);
                rectTransform.anchorMax = new Vector2(0, 0);
                rectTransform.sizeDelta = new Vector2(distance, 3f);
                rectTransform.anchoredPosition = dotPositionA + dir * distance * 0.5f;
                rectTransform.localEulerAngles = new Vector3(0, 0, Utils.GetAngleFromVectorFloat(dir));
                return gameObject;
            }
        }
    }
}