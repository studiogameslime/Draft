using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using UnityEngine.Events;
using System.Collections.Generic;
using GoogleMobileAds.Api;

namespace EasyUI.PickerWheelUI
{
    public class PickerWheel : MonoBehaviour
    {
        [Header("Data (your definitions)")]
        [SerializeField] private FortuneWheelDatabase database;

        [Header("Managers")]
        [SerializeField] private WheelOfFortuneTokensManager tokensManager;

        [Header("References")]
        [SerializeField] private GameObject linePrefab;
        [SerializeField] private Transform linesParent;

        [Space]
        [SerializeField] private Transform pickerWheelTransform;
        [SerializeField] private Transform wheelCircle;
        [SerializeField] private GameObject wheelPiecePrefab;
        [SerializeField] private Transform wheelPiecesParent;

        [Header("Picker wheel settings")]
        [Range(1, 20)] public int spinDuration = 8;
        [SerializeField, Range(.2f, 2f)] private float wheelSize = 1f;

        // Events
        private UnityAction onSpinStartEvent;
        private UnityAction<WheelPiece> onSpinEndEvent;

        private bool _isSpinning;
        public bool IsSpinning => _isSpinning;

        private readonly Vector2 pieceMinSize = new Vector2(81f, 146f);
        private readonly Vector2 pieceMaxSize = new Vector2(144f, 213f);
        private const int piecesMin = 2;
        private const int piecesMax = 12;

        private float pieceAngle;
        private float halfPieceAngle;
        private float halfPieceAngleWithPaddings;

        // Runtime built from database order (possibly shuffled)
        private FortuneWheelSlotDefinition[] _slotByIndex;
        private RectTransform[] _iconRectByIndex;

        // WheelPiece array used only for rendering text/icon/amount
        [SerializeField] private WheelPiece[] wheelPieces;

        private bool _generatedOnce;

        private void Start()
        {
            RebuildFromDatabase(randomizeOrder: false);
        }

        // Call this from popup every time you open the wheel to shuffle positions.
        public void RebuildFromDatabase(bool randomizeOrder)
        {
            if (database == null || database.slots == null || database.slots.Count == 0)
            {
                Debug.LogError("PickerWheel: Database is missing or empty.");
                return;
            }

            ClearGeneratedVisuals();
            BuildFromDatabase(randomizeOrder);

            pieceAngle = 360f / wheelPieces.Length;
            halfPieceAngle = pieceAngle / 2f;
            halfPieceAngleWithPaddings = halfPieceAngle - (halfPieceAngle / 4f);

            GenerateVisuals();

            _generatedOnce = true;
        }

        private void BuildFromDatabase(bool randomizeOrder)
        {
            int count = Mathf.Clamp(database.slots.Count, piecesMin, piecesMax);

            // Build order list so we can shuffle without touching database order.
            List<int> order = new List<int>(count);
            for (int i = 0; i < count; i++) order.Add(i);
            if (randomizeOrder) Shuffle(order);

            wheelPieces = new WheelPiece[count];
            _slotByIndex = new FortuneWheelSlotDefinition[count];
            _iconRectByIndex = new RectTransform[count];

            for (int i = 0; i < count; i++)
            {
                FortuneWheelSlotDefinition def = database.slots[order[i]];
                _slotByIndex[i] = def;

                WheelPiece p = new WheelPiece();
                p.Icon = def != null ? def.icon : null;
                p.Label = def != null ? def.displayName : "Slot";
                
                p.Amount = (def != null && def.reward != null) ? def.reward.amount : 0;
                if(def != null && def.reward != null && def.reward.type == FortuneRewardType.Gold)
                {
                    p.Amount = MasteryBonusManager.Instance.GetBoostedGold(def.reward.amount);

                }
                // Chance is not used anymore (rarity is rolled from DB).
                p.Chance = 100f;

                wheelPieces[i] = p;
            }
        }

        private void GenerateVisuals()
        {
            if (wheelPiecePrefab == null)
            {
                Debug.LogError("PickerWheel: wheelPiecePrefab is not assigned.");
                return;
            }

            // Resize content once
            GameObject temp = Instantiate(wheelPiecePrefab, wheelPiecesParent.position, Quaternion.identity, wheelPiecesParent);
            RectTransform rt = temp.transform.GetChild(0).GetComponent<RectTransform>();

            float t = 1f - Mathf.InverseLerp(piecesMin, piecesMax, wheelPieces.Length);
            float pieceWidth = Mathf.Lerp(pieceMinSize.x, pieceMaxSize.x, t);
            float pieceHeight = Mathf.Lerp(pieceMinSize.y, pieceMaxSize.y, t);

            rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, pieceWidth);
            rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, pieceHeight);

            Destroy(temp);

            // Draw
            for (int i = 0; i < wheelPieces.Length; i++)
                DrawPiece(i);
        }

        private void DrawPiece(int index)
        {
            WheelPiece piece = wheelPieces[index];

            Transform pieceRoot = Instantiate(wheelPiecePrefab, wheelPiecesParent.position, Quaternion.identity, wheelPiecesParent).transform.GetChild(0);

            // Package hierarchy:
            // 0 = Icon Image
            // 1 = Label Text
            // 2 = Amount Text
            Image iconImg = pieceRoot.GetChild(1).GetComponent<Image>();
            Text labelTxt = pieceRoot.GetChild(0).GetComponent<Text>();
            Text amountTxt = pieceRoot.GetChild(2).GetComponent<Text>();

            if (iconImg != null) iconImg.sprite = piece.Icon;
            if (labelTxt != null) labelTxt.text = piece.Label;
            if (amountTxt != null) amountTxt.text = piece.Amount.ToString();

            // Save icon rect for reward fly FX origin
            if (iconImg != null)
                _iconRectByIndex[index] = iconImg.rectTransform;

            // Line
            Transform lineTrns = Instantiate(linePrefab, linesParent.position, Quaternion.identity, linesParent).transform;
            lineTrns.RotateAround(wheelPiecesParent.position, Vector3.back, (pieceAngle * index) + halfPieceAngle);

            // Rotate piece into place
            pieceRoot.RotateAround(wheelPiecesParent.position, Vector3.back, pieceAngle * index);
        }

        private void ClearGeneratedVisuals()
        {
            for (int i = wheelPiecesParent.childCount - 1; i >= 0; i--)
                Destroy(wheelPiecesParent.GetChild(i).gameObject);

            for (int i = linesParent.childCount - 1; i >= 0; i--)
                Destroy(linesParent.GetChild(i).gameObject);

            if (wheelCircle != null)
                wheelCircle.localRotation = Quaternion.identity;

            _generatedOnce = false;
            _slotByIndex = null;
            _iconRectByIndex = null;
            wheelPieces = null;
        }

        public void Spin()
        {
            if (!_generatedOnce)
            {
                Debug.LogWarning("PickerWheel: Spin called before wheel was generated.");
                return;
            }

            if (_isSpinning) return;

            // 1) Check token and consume it
            if (tokensManager == null)
            {
                Debug.LogWarning("PickerWheel: tokensManager is not assigned.");
                return;
            }

            if (!tokensManager.TryConsumeSpinToken(1))
                return;

            // 2) Roll rarity from DB chances
            FortuneRewardRarity rolledRarity = RollRarityFromDatabase();

            // 3) Pick random slot index that matches the rolled rarity
            int index = PickRandomIndexByRarity(rolledRarity);
            if (index < 0) return;

            FortuneWheelSlotDefinition pickedDef = _slotByIndex[index];

            // 4) Spin animation to the chosen index
            _isSpinning = true;
            onSpinStartEvent?.Invoke();

            float angle = -(pieceAngle * index);
            float rightOffset = (angle - halfPieceAngleWithPaddings) % 360f;
            float leftOffset = (angle + halfPieceAngleWithPaddings) % 360f;
            float randomAngle = Random.Range(leftOffset, rightOffset);

            Vector3 targetRotation = Vector3.back * (randomAngle + 2f * 360f * spinDuration);

            wheelCircle
                .DORotate(targetRotation, spinDuration, RotateMode.Fast)
                .SetEase(Ease.InOutQuart)
                .OnComplete(() =>
                {
                    _isSpinning = false;

                    // 5) Grant reward: fromRect is the winning icon rect
                    RectTransform fromRect =
                        (_iconRectByIndex != null && index >= 0 && index < _iconRectByIndex.Length)
                            ? _iconRectByIndex[index]
                            : null;

                    tokensManager.GrantReward(pickedDef, fromRect);

                    // Optional: keep the package event too
                    if (onSpinEndEvent != null && index >= 0 && index < wheelPieces.Length)
                        onSpinEndEvent.Invoke(wheelPieces[index]);

                    onSpinStartEvent = null;
                    onSpinEndEvent = null;
                });
        }

        private FortuneRewardRarity RollRarityFromDatabase()
        {
            if (database == null)
                return FortuneRewardRarity.Common;

            database.GetNormalizedRarityChances(out float c, out float r, out float e);

            float roll = Random.value; // 0..1
            if (roll < c) return FortuneRewardRarity.Common;
            if (roll < c + r) return FortuneRewardRarity.Rare;
            return FortuneRewardRarity.Epic;
        }

        private int PickRandomIndexByRarity(FortuneRewardRarity rarity)
        {
            if (_slotByIndex == null || _slotByIndex.Length == 0)
                return -1;

            List<int> candidates = new List<int>(_slotByIndex.Length);

            for (int i = 0; i < _slotByIndex.Length; i++)
            {
                FortuneWheelSlotDefinition def = _slotByIndex[i];
                if (def == null || def.reward == null) continue;

                if (def.reward.rarity == rarity)
                    candidates.Add(i);
            }

            // Fallback: if no slots exist for that rarity, pick any valid slot
            if (candidates.Count == 0)
            {
                for (int i = 0; i < _slotByIndex.Length; i++)
                {
                    if (_slotByIndex[i] != null && _slotByIndex[i].reward != null)
                        candidates.Add(i);
                }
            }

            if (candidates.Count == 0)
                return -1;

            return candidates[Random.Range(0, candidates.Count)];
        }

        public void OnSpinStart(UnityAction action) => onSpinStartEvent = action;
        public void OnSpinEnd(UnityAction<WheelPiece> action) => onSpinEndEvent = action;

        private void Shuffle(List<int> list)
        {
            // Fisher-Yates
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                int tmp = list[i];
                list[i] = list[j];
                list[j] = tmp;
            }
        }

        private void OnValidate()
        {
            if (pickerWheelTransform != null)
                pickerWheelTransform.localScale = new Vector3(wheelSize, wheelSize, 1f);
        }
    }
}
