using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using Microsoft.MixedReality.Toolkit.UX;

namespace EvanZ.Tools
{
    public class Window_Graph : MonoBehaviour
    {
        private static Window_Graph Instance;

        [SerializeField] private RectTransform _graphContainer;
        [SerializeField] private Sprite _dotSprite;
        [SerializeField] private GameObject _labelTemplateX;
        [SerializeField] private GameObject _labelTemplateY;
        [SerializeField] private GameObject _dashTemplateX;
        [SerializeField] private GameObject _dashTemplateY;
        [SerializeField] private GameObject _toolTip;

        private List<GameObject> _gameObjectsList;
        private List<IGraphVisualObject> _graphVisualObjectsList;
        private List<RectTransform> _yLabelList;

        // Cached values
        private List<float> _valueList;
        private IGraphVisual _graphVisual;
        private int _maxVisibleValueAmount = -1;
        private Func<string, string> _getAxisLabelX = null;
        private Func<float, string> _getAxisLabelY = null;
        private float _xSize;

        private bool _customizeStartEnd;
        private float _startYAt;
        private float _endYAt;
        private float _xScaleMultiplier;

        private bool _useHorizontalDash;

        private BarChartVisual barChartVisual;
        private LineGraphVisual lineGraphVisual;

        private void Awake()
        {
            Instance = this;

            _toolTip.SetActive(false);
            _gameObjectsList = new();
            _graphVisualObjectsList = new();
            _yLabelList = new();
            _customizeStartEnd = false;
            _startYAt = -1;
            _endYAt = -1;
            _xScaleMultiplier = 30;
            _useHorizontalDash = true;

            _valueList = new List<float>() {15, 2, 23, 2};

            lineGraphVisual = new(_graphContainer, _dotSprite, Color.white, Color.white);
            barChartVisual = new(_graphContainer, Color.green, .9f);

            ShowGraph(_valueList, lineGraphVisual, -1);
        }

        public void UpdateValueList(List<float> values)
        {
            _valueList = values;
            ShowGraph(_valueList, _graphVisual, -1);
        }

        public void SetGraphVisualToBar()
        {
            ShowGraph(_valueList, barChartVisual, _maxVisibleValueAmount, _getAxisLabelX, _getAxisLabelY);
        }
        public void SetGraphVisualToLine()
        {
            ShowGraph(_valueList, lineGraphVisual, _maxVisibleValueAmount, _getAxisLabelX, _getAxisLabelY);
        }
        public void SetHorizontalDash(bool useHorizontalDash)
        {
            _useHorizontalDash = useHorizontalDash;
        }
        public void IncreaseVisibleAmount()
        {
            ShowGraph(_valueList, _graphVisual, _maxVisibleValueAmount + 1, _getAxisLabelX, _getAxisLabelY);
        }
        public void DecreaseVisibleAmount()
        {
            ShowGraph(_valueList, _graphVisual, _maxVisibleValueAmount - 1, _getAxisLabelX, _getAxisLabelY);
        }
        public void UseIntY(string unit)
        {
            _getAxisLabelY = (float _f) => { return Mathf.RoundToInt(_f).ToString() + " " + unit; };
        }
        public void UseFloatY(string unit)
        {
            _getAxisLabelY = (float _f) => { return $"{_f:F2}" + " " + unit; };
        }
        public void ChangeLabelXUnit(string unit)
        {
            _getAxisLabelX = (string _i) => { return _i + unit; };
        }
        public void ChangeXScaleMultiplier(float scale)
        {
            _xScaleMultiplier = scale;
        }
        public void UseCustomYScale(bool useCustom, float yStart, float yEnd)
        {
            _customizeStartEnd = useCustom;
            _startYAt = yStart;
            _endYAt = yEnd;
        }

        public static void ShowToolTip_Static(string tooltipText, Vector2 anchoredPosition)
        {
            Instance.ShowToolTip(tooltipText, anchoredPosition);
        }

        private void ShowToolTip(string tooltipText, Vector2 anchoredPosition)
        {
            _toolTip.SetActive(true);
            float textPaddingSize = 4f;
            TMP_Text textComponent = _toolTip.GetComponentInChildren<TMP_Text>();
            textComponent.text = tooltipText;
            Vector2 backgroundSize = new Vector2(
                textComponent.preferredWidth + textPaddingSize * 2f,
                textComponent.preferredHeight + textPaddingSize * 2f
            );
            _toolTip.transform.Find("Background").GetComponent<RectTransform>().sizeDelta = backgroundSize;
            _toolTip.GetComponent<RectTransform>().anchoredPosition = anchoredPosition;
            _toolTip.transform.SetAsLastSibling();
        }

        public static void HideToolTip_Static()
        {
            Instance.HideToolTip();
        }

        private void HideToolTip()
        {
            _toolTip.SetActive(false);
        }

        private void ShowGraph(List<float> valueList, IGraphVisual graphVisual,
            int maxVisibleValueAmount = -1, Func<string, string> getAxisLabelX = null, Func<float, string> getAxisLabelY = null)
        {
            _valueList = valueList;
            _graphVisual = graphVisual;

            if (getAxisLabelX == null)
            {
                getAxisLabelX = (string _i) => { return _i; };
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
            _yLabelList.Clear();
            foreach (IGraphVisualObject graphVisualObject in _graphVisualObjectsList)
            {
                graphVisualObject.CleanUp();
            }
            _graphVisualObjectsList.Clear();

            float graphWidth = _graphContainer.sizeDelta.x;
            float graphHeight = _graphContainer.sizeDelta.y;

            float yMinimum, yMaximum;
            CalculateYScale(out yMinimum, out yMaximum);

            _xSize = graphWidth / (_maxVisibleValueAmount + 1);
            int xIndex = 0;

            if (_graphVisual is LineGraphVisual visual)
                visual.ResetLastDotGameObject();

            for (int i = Mathf.Max(_valueList.Count - _maxVisibleValueAmount, 0); i < _valueList.Count; i++)
            {
                float xPosition = _xSize + xIndex * _xSize;
                float yPosition = (_valueList[i] - yMinimum) / (yMaximum - yMinimum) * graphHeight;

                string toolTipText = _valueList[i].ToString();
                _graphVisualObjectsList.Add(_graphVisual.CreatGraphVisualObject(new Vector2(xPosition, yPosition), _xSize, toolTipText));

                RectTransform labelX = Instantiate(_labelTemplateX).GetComponent<RectTransform>();
                int segments = _valueList.Count - 1 - i;
                string displayTime;
                if (segments == 0)
                {
                    displayTime = "Now";
                }
                else 
                {
                    TimeSpan time = TimeSpan.FromSeconds(segments * _xScaleMultiplier);
                    DateTime dateTime = DateTime.Today.Add(time);
                    displayTime = "-" + dateTime.ToString("mm:ss");
                }

                labelX.SetParent(_graphContainer, false);
                labelX.anchoredPosition = new Vector2(xPosition, 0f);
                labelX.GetComponent<TMP_Text>().text = _getAxisLabelX(displayTime);
                _gameObjectsList.Add(labelX.gameObject);

                if (_useHorizontalDash)
                {
                    RectTransform dashX = Instantiate(_dashTemplateX).GetComponent<RectTransform>();
                    dashX.SetParent(_graphContainer, false);
                    dashX.anchoredPosition = new Vector2(xPosition, 0);
                    dashX.sizeDelta = new Vector2(_graphContainer.sizeDelta.y, dashX.sizeDelta.y);
                    _gameObjectsList.Add(dashX.gameObject);
                }

                xIndex++;
            }

            int separatorCount = 10;
            for (int i = 0; i <= separatorCount; i++)
            {
                RectTransform labelY = Instantiate(_labelTemplateY).GetComponent<RectTransform>();
                labelY.SetParent(_graphContainer, false);
                float normalizedValue = i * 1f / separatorCount;
                labelY.anchoredPosition = new Vector2(-25f, normalizedValue * graphHeight);
                labelY.GetComponent<TMP_Text>().text = _getAxisLabelY(yMinimum + normalizedValue * (yMaximum - yMinimum));
                _gameObjectsList.Add(labelY.gameObject);
                _yLabelList.Add(labelY);

                RectTransform dashY = Instantiate(_dashTemplateY).GetComponent<RectTransform>();
                dashY.SetParent(_graphContainer, false);
                dashY.anchoredPosition = new Vector2(4.3f, normalizedValue * graphHeight);
                dashY.sizeDelta = new Vector2(_graphContainer.sizeDelta.x - 4.3f, dashY.sizeDelta.y);
                _gameObjectsList.Add(dashY.gameObject);
            }
            if (_useHorizontalDash)
            {
                RectTransform dashX = Instantiate(_dashTemplateX).GetComponent<RectTransform>();
                dashX.SetParent(_graphContainer, false);
                dashX.anchoredPosition = new Vector2(_graphContainer.sizeDelta.x, 0);
                dashX.sizeDelta = new Vector2(_graphContainer.sizeDelta.y, dashX.sizeDelta.y);
                _gameObjectsList.Add(dashX.gameObject);

                dashX = Instantiate(_dashTemplateX).GetComponent<RectTransform>();
                dashX.SetParent(_graphContainer, false);
                dashX.anchoredPosition = new Vector2(4.3f, 0);
                dashX.sizeDelta = new Vector2(_graphContainer.sizeDelta.y, dashX.sizeDelta.y);
                _gameObjectsList.Add(dashX.gameObject);
            }
        }

        public void UpdateValue(int index, float value)
        {
            float yMinimumBefore, yMaximumBefore;
            CalculateYScale(out yMinimumBefore, out yMaximumBefore);

            _valueList[index] = value;

            float graphWidth = _graphContainer.sizeDelta.x;
            float graphHeight = _graphContainer.sizeDelta.y;

            float yMinimum, yMaximum;
            CalculateYScale(out yMinimum, out yMaximum);

            bool yScaleChanged = yMinimumBefore != yMinimum || yMaximumBefore != yMaximum;

            if (!yScaleChanged)
            {
                float xPosition = _xSize + index * _xSize;
                float yPosition = (value - yMinimum) / (yMaximum - yMinimum) * graphHeight;

                string toolTipText = value.ToString();
                _graphVisualObjectsList[index].SetGraphVisualObjectInfo(new Vector2(xPosition, yPosition), _xSize, toolTipText);
            }
            else
            {
                int xIndex = 0;

                if (_graphVisual is LineGraphVisual visual)
                    visual.ResetLastDotGameObject();

                for (int i = Mathf.Max(_valueList.Count - _maxVisibleValueAmount, 0); i < _valueList.Count; i++)
                {
                    float xPosition = _xSize + xIndex * _xSize;
                    float yPosition = (_valueList[i] - yMinimum) / (yMaximum - yMinimum) * graphHeight;

                    string toolTipText = _valueList[i].ToString();
                    _graphVisualObjectsList[xIndex].SetGraphVisualObjectInfo(new Vector2(xPosition, yPosition), _xSize, toolTipText);

                    xIndex++;
                }

                for (int i = 0; i < _yLabelList.Count; i++)
                {
                    float normalizedValue = i * 1f / _yLabelList.Count;
                    _yLabelList[i].GetComponent<TMP_Text>().text = _getAxisLabelY(yMinimum + normalizedValue * (yMaximum - yMinimum));
                }
            }
        }

        private void CalculateYScale(out float yMinimum, out float yMaximum)
        {
            yMaximum = _valueList[0];
            yMinimum = _valueList[0];
            for (int i = Mathf.Max(_valueList.Count - _maxVisibleValueAmount, 0); i < _valueList.Count; i++)
            {
                float value = _valueList[i];
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

            if (_customizeStartEnd)
            {
                yMinimum = _startYAt;
                yMaximum = _endYAt;
            }
        }

        private interface IGraphVisual
        {
            IGraphVisualObject CreatGraphVisualObject(Vector2 graphPosition, float graphPositionWidth, string toolTipText);
        }

        private interface IGraphVisualObject
        {
            void SetGraphVisualObjectInfo(Vector2 graphPosition, float graphPositionWidth, string toolTipText);
            void CleanUp();
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

            public IGraphVisualObject CreatGraphVisualObject(Vector2 graphPosition, float graphPositionWidth, string toolTipText)
            {
                GameObject barGameObject = CreateBar(graphPosition, graphPositionWidth * _barWidthMultiplier);

                BarChartViusalObject barChartViusalObject = new(barGameObject, _barWidthMultiplier);
                barChartViusalObject.SetGraphVisualObjectInfo(graphPosition, graphPositionWidth, toolTipText);

                return barChartViusalObject;
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

                gameObject.AddComponent<BoxCollider>();
                gameObject.AddComponent<PressableButton>();
                return gameObject;
            }
        }

        private class LineGraphVisual : IGraphVisual
        {
            private RectTransform _graphContainer;
            private Sprite _dotSprite;
            private LineGraphVisualObject _lastLineGraphVisualObject;
            private Color _dotColor;
            private Color _dotConnectionColor;

            public LineGraphVisual(RectTransform graphContainer, Sprite dotSprite, Color dotColor, Color dotConnectionColor)
            {
                _graphContainer = graphContainer;
                _dotSprite = dotSprite;
                _lastLineGraphVisualObject = null;
                _dotColor = dotColor;
                _dotConnectionColor = dotConnectionColor;
            }

            public void ResetLastDotGameObject() => _lastLineGraphVisualObject = null;

            public IGraphVisualObject CreatGraphVisualObject(Vector2 graphPosition, float graphPositionWidth, string toolTipText)
            {
                List<GameObject> gameObjectsList = new();
                GameObject dotGameObject = CreateDot(graphPosition);

                gameObjectsList.Add(dotGameObject);
                GameObject dotConnectionGameObject = null;
                if (_lastLineGraphVisualObject != null)
                {
                    dotConnectionGameObject = CreateDotConnection(_lastLineGraphVisualObject.GetGraphPosition(),
                        dotGameObject.GetComponent<RectTransform>().anchoredPosition);
                    gameObjectsList.Add(dotConnectionGameObject);
                }

                LineGraphVisualObject lineGraphVisualObject = new(dotGameObject, dotConnectionGameObject, _lastLineGraphVisualObject);
                lineGraphVisualObject.SetGraphVisualObjectInfo(graphPosition, graphPositionWidth, toolTipText);

                _lastLineGraphVisualObject = lineGraphVisualObject;

                return lineGraphVisualObject;
            }

            private GameObject CreateDot(Vector2 anchoredPosition)
            {
                GameObject gameObject = new GameObject("dot", typeof(Image));
                gameObject.transform.SetParent(_graphContainer, false);
                gameObject.GetComponent<Image>().sprite = _dotSprite;
                gameObject.GetComponent<Image>().color = _dotColor;
                RectTransform rectTransform = gameObject.GetComponent<RectTransform>();
                rectTransform.anchoredPosition = anchoredPosition;
                rectTransform.sizeDelta = new Vector2(6, 6);
                rectTransform.anchorMin = new Vector2(0, 0);
                rectTransform.anchorMax = new Vector2(0, 0);

                gameObject.AddComponent<BoxCollider>();
                gameObject.AddComponent<PressableButton>();
                return gameObject;
            }

            private GameObject CreateDotConnection(Vector2 dotPositionA, Vector2 dotPositionB)
            {
                GameObject gameObject = new GameObject("dotConnection", typeof(Image));
                gameObject.transform.SetParent(_graphContainer, false);
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

        public class BarChartViusalObject : IGraphVisualObject
        {
            private GameObject _barGameObject;
            private float _barWidthMultiplier;

            public BarChartViusalObject(GameObject barGameObject, float barWidthMultiplier)
            {
                _barGameObject = barGameObject;
                _barWidthMultiplier = barWidthMultiplier;
            }

            public void SetGraphVisualObjectInfo(Vector2 graphPosition, float graphPositionWidth, string toolTipText)
            {
                RectTransform rectTransform = _barGameObject.GetComponent<RectTransform>();
                rectTransform.anchoredPosition = new Vector2(graphPosition.x, 0f);
                rectTransform.sizeDelta = new Vector2(graphPositionWidth * _barWidthMultiplier, graphPosition.y);

                BoxCollider boxCollider = _barGameObject.GetComponent<BoxCollider>();
                boxCollider.size = new Vector3(rectTransform.sizeDelta.x, rectTransform.sizeDelta.y, 1.0f);
                boxCollider.center = new Vector3(0, rectTransform.sizeDelta.y / 2, 0);
                PressableButton pressableButton = _barGameObject.GetComponent<PressableButton>();
                pressableButton.IsGazeHovered.OnEntered.AddListener((t) => ShowToolTip_Static(toolTipText, graphPosition));
                pressableButton.IsGazeHovered.OnExited.AddListener((t) => HideToolTip_Static());
            }

            public void CleanUp()
            {
                Destroy(_barGameObject);
            }
        }

        public class LineGraphVisualObject : IGraphVisualObject
        {
            public event EventHandler OnChangeGraphVisualObjectInfo;
            private GameObject _dotGameObject;
            private GameObject _dotConnectionGameObject;
            private LineGraphVisualObject _lastVisualObject;
            public LineGraphVisualObject(GameObject dotGameObject, GameObject dotConnectionGameObject, LineGraphVisualObject lastVisualObject)
            {
                _dotGameObject = dotGameObject;
                _dotConnectionGameObject = dotConnectionGameObject;
                _lastVisualObject = lastVisualObject;

                if (_lastVisualObject != null)
                {
                    _lastVisualObject.OnChangeGraphVisualObjectInfo += LastVisualObject_OnChangeGraphVisualObjectInfo;
                }
            }

            private void LastVisualObject_OnChangeGraphVisualObjectInfo(object sender, EventArgs e)
            {
                UpdateDotConnection();
            }

            public void SetGraphVisualObjectInfo(Vector2 graphPosition, float graphPositionWidth, string toolTipText)
            {
                RectTransform rectTransform = _dotGameObject.GetComponent<RectTransform>();
                rectTransform.anchoredPosition = graphPosition;

                UpdateDotConnection();

                BoxCollider boxCollider = _dotGameObject.GetComponent<BoxCollider>();
                boxCollider.size = new Vector3(rectTransform.sizeDelta.x, rectTransform.sizeDelta.y, 1.0f);
                boxCollider.center = new Vector3(0, 0, 0);
                PressableButton pressableButton = _dotGameObject.GetComponent<PressableButton>();
                pressableButton.IsGazeHovered.OnEntered.AddListener((t) => ShowToolTip_Static(toolTipText, graphPosition));
                pressableButton.IsGazeHovered.OnExited.AddListener((t) => HideToolTip_Static());

                OnChangeGraphVisualObjectInfo?.Invoke(this, EventArgs.Empty);
            }

            public void CleanUp()
            {
                Destroy(_dotGameObject);
                Destroy(_dotConnectionGameObject);
            }

            public Vector2 GetGraphPosition()
            {
                RectTransform rectTransform = _dotGameObject.GetComponent<RectTransform>();
                return rectTransform.anchoredPosition;
            }

            private void UpdateDotConnection()
            {
                if (_dotConnectionGameObject != null)
                {
                    RectTransform dotConnectionRectTransform = _dotConnectionGameObject.GetComponent<RectTransform>();
                    Vector2 dir = (_lastVisualObject.GetGraphPosition() - GetGraphPosition()).normalized;
                    float distance = Vector2.Distance(GetGraphPosition(), _lastVisualObject.GetGraphPosition());
                    dotConnectionRectTransform.anchorMin = new Vector2(0, 0);
                    dotConnectionRectTransform.anchorMax = new Vector2(0, 0);
                    dotConnectionRectTransform.sizeDelta = new Vector2(distance, 1.6f);
                    dotConnectionRectTransform.anchoredPosition = GetGraphPosition() + dir * distance * 0.5f;
                    dotConnectionRectTransform.localEulerAngles = new Vector3(0, 0, Utils.GetAngleFromVectorFloat(dir));
                }
            }
        }
    }
}

