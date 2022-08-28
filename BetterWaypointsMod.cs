using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using HarmonyLib;
using System.Reflection;
using UnityEngine.UI;

namespace Exund.BetterWaypoints
{
    public class BetterWaypointsMod : ModBase
    {
        internal const string HarmonyID = "exund.betterwaypoints";
        internal static Harmony harmony = new Harmony(HarmonyID);

        public override bool HasEarlyInit()
        {
            return true;
        }

        public override void Init()
        {
            harmony.PatchAll(Assembly.GetExecutingAssembly());
        }

        public override void DeInit()
        {
            harmony.UnpatchAll(HarmonyID);
        }
    }

    static class Patches
    {
        [HarmonyPatch(typeof(ManRadar), "GetRadarMarkerColor")]
        private static class DebugUtilEnabled
        {
            private static void Postfix(ref Color __result, ManRadar.RadarMarkerColorType colorType)
            {
                if (!Enum.IsDefined(typeof(ManRadar.RadarMarkerColorType), colorType))
                {
                    int num = (int)colorType & ~(1 << 25);
                    Color32 c = new Color32
                    {
                        r = (byte)((num >> 0) & 0xFF),
                        g = (byte)((num >> 8) & 0xFF),
                        b = (byte)((num >> 16) & 0xFF),
                        a = 255
                    };
                    __result = c;
                }
            }
        }

        [HarmonyPatch(typeof(ManUI), "Start")]

        private static class ManUIStart
        {
            private static readonly FieldInfo m_SelectedColor = typeof(UIScreenRenameTech).GetField("m_SelectedColor", BindingFlags.Instance | BindingFlags.NonPublic);

            private static void Postfix()
            {
                UIScreen renameScreen = Singleton.Manager<ManUI>.inst.GetScreen(ManUI.ScreenType.RenameTech_MarkerBlock);

                var inputField = renameScreen.transform.Find("Rename/Name _InputField");
                var colorsPanel = renameScreen.transform.Find("IconSwatchPanel/Colours");

                colorsPanel.Find("ColoursGrid").gameObject.SetActive(false);

                var colorInput = GameObject.Instantiate(inputField, colorsPanel);
                GameObject.DestroyImmediate(colorInput.GetComponentInChildren<UILocalisedText>());
                var input = colorInput.GetComponent<InputField>();
                input.placeholder.GetComponent<Text>().text = "RRGGBB";
                input.onValueChanged.RemoveAllListeners();
                input.onEndEdit.RemoveAllListeners();
                input.readOnly = false;
                input.interactable = true;
                input.characterValidation = InputField.CharacterValidation.None;
                input.contentType = InputField.ContentType.Standard;
                input.onValidateInput = null;
                input.ActivateInputField();

                var nav = input.navigation; 
                nav.mode = Navigation.Mode.Automatic;
                input.navigation = nav;
                var inputRect = colorInput.GetComponent<RectTransform>();
                inputRect.anchoredPosition = new Vector2(0, -5f);
                inputRect.anchorMin = new Vector2(0, 0.6706005f);
                inputRect.anchorMax = new Vector2(1, 0.8973996f);

                var colorPreview = GameObject.Instantiate(colorsPanel.Find("ColoursGrid/Colour01"), colorsPanel);
                GameObject.DestroyImmediate(colorPreview.GetComponent<RadarMarkerEditButton>());
                var button = colorPreview.GetComponent<Button>();
                var image = colorPreview.Find("ColourSwatch01").GetComponent<Image>();
                button.onClick.RemoveAllListeners();

                var colorRect = colorPreview.GetComponent<RectTransform>();
                colorRect.anchorMin = new Vector2(0, 0.6706005f);
                colorRect.anchorMax = new Vector2(1, 0.8973996f);
                colorRect.anchoredPosition = new Vector2(0, -60f);

                button.onClick.AddListener(() =>
                {
                    input.Select();
                    input.ActivateInputField();
                });

                input.onEndEdit.AddListener((str) =>
                {
                    if (System.Text.RegularExpressions.Regex.IsMatch(str, "([a-fA-F0-9]{6}|[a-fA-F0-9]{3})"))
                    {
                        str = "#" + str;
                    }
                    if (ColorUtility.TryParseHtmlString(str, out Color c))
                    {
                        Color32 c32 = c;
                        int v = (1 << 25) | c32.r | c32.g << 8 | c32.b << 16;
                        m_SelectedColor.SetValue(renameScreen, (ManRadar.RadarMarkerColorType)v);
                        image.color = c;
                    }
                });
            }
        }
    }
}
