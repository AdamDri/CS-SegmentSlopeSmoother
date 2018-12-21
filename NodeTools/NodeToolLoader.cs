using ICities;
using UnityEngine;
using ColossalFramework.UI;

namespace NodeTools
{
    public class NodeToolLoader : LoadingExtensionBase
    {
        public override void OnLevelLoaded(LoadMode mode)
        {
            // initiate tool
            if (NodeSelectionTool.instance == null)
            {
                ToolController toolController = GameObject.FindObjectOfType<ToolController>();

                NodeSelectionTool.instance = toolController.gameObject.AddComponent<NodeSelectionTool>();
                NodeSelectionTool.instance.enabled = false;
            }

            // initiate ui
            UIComponent TSBar = UIView.Find("TSBar");
            UIPanel elektrixModsBackground = TSBar.AddUIComponent<UIPanel>();
            elektrixModsBackground.name = "ElektrixMB";
            elektrixModsBackground.backgroundSprite = "GenericPanelLight";
            elektrixModsBackground.position = new Vector3(185f, 0f);
            elektrixModsBackground.width = 60f;
            elektrixModsBackground.height = 60f;
            elektrixModsBackground.color = new Color32(96, 96, 96, 255);

            UIButton elektrixModsToggle = elektrixModsBackground.AddUIComponent<UIButton>();
            int toggleClicks = 0;
            elektrixModsToggle.normalBgSprite = "ToolbarIconGroup1Normal";
            elektrixModsToggle.hoveredBgSprite = "ToolbarIconGroup1Hovered";
            elektrixModsToggle.pressedBgSprite = "ToolbarIconGroup1Pressed";
            elektrixModsToggle.disabledBgSprite = "ToolbarIconGroup1Disabled";
            elektrixModsToggle.focusedBgSprite = "ToolbarIconGroup1Focused";
            elektrixModsToggle.disabledTextColor = new Color32(128, 128, 128, 255);

            elektrixModsToggle.relativePosition = new Vector3(5f, 0f);
            elektrixModsToggle.size = new Vector2(50f, 50f);

            elektrixModsToggle.name = "ElektrixModsButton";
            elektrixModsToggle.text = "E";
            elektrixModsToggle.textScale = 1.3f;
            elektrixModsToggle.textVerticalAlignment = UIVerticalAlignment.Middle;
            elektrixModsToggle.textHorizontalAlignment = UIHorizontalAlignment.Center;

            float panelHeight = 100f;
            UIPanel modsPanel = elektrixModsBackground.AddUIComponent<UIPanel>();
            modsPanel.backgroundSprite = "GenericPanelLight";
            modsPanel.color = new Color32(96, 96, 96, 255);
            modsPanel.name = "ElektrixModsPanel";
            modsPanel.height = panelHeight;
            modsPanel.width = 155f;
            modsPanel.relativePosition = new Vector3(0, -panelHeight - 7);
            modsPanel.Hide();

            UILabel panelLabel = modsPanel.AddUIComponent<UILabel>();
            panelLabel.text = "Elektrix's Mods";
            panelLabel.relativePosition = new Vector3(15f, 15f);

            UIButton slopeTool = modsPanel.AddUIComponent<UIButton>();
            int slopeClicks = 0;
            slopeTool.normalBgSprite = "OptionBase";
            slopeTool.hoveredBgSprite = "OptionBaseHovered";
            slopeTool.pressedBgSprite = "OptionBasePressed";
            slopeTool.disabledBgSprite = "OptionBaseDisabled";
            slopeTool.focusedBgSprite = "OptionBaseFocused";
            slopeTool.size = new Vector2(45f, 45f);
            slopeTool.relativePosition = new Vector3(15f, 40f);
            slopeTool.name = "ElektrixSlopeToolButton";
            slopeTool.text = "S";
            slopeTool.textScale = 1.3f;

            UIButton slopeToolSubmit = modsPanel.AddUIComponent<UIButton>();
            slopeToolSubmit.normalBgSprite = "OptionBase";
            slopeToolSubmit.hoveredBgSprite = "OptionBaseHovered";
            slopeToolSubmit.pressedBgSprite = "OptionBasePressed";
            slopeToolSubmit.disabledBgSprite = "OptionBaseDisabled";
            slopeToolSubmit.focusedBgSprite = "OptionBaseFocused";
            slopeToolSubmit.size = new Vector2(45f, 45f);
            slopeToolSubmit.relativePosition = new Vector3(65f, 40f);
            slopeToolSubmit.name = "ElektrixSlopeSubmitButton";
            slopeToolSubmit.text = "Go";
            slopeToolSubmit.textScale = 1.3f;


            // Events
            elektrixModsToggle.eventClicked += (component, click) =>
            {
                toggleClicks++;
                if (toggleClicks == 1)
                {
                    elektrixModsToggle.Focus();
                    modsPanel.Show();
                }
                else
                {
                    elektrixModsToggle.Unfocus();
                    toggleClicks = 0;
                    modsPanel.Hide();
                }
            };
            slopeTool.eventClicked += (component, click) =>
            {
                slopeClicks++;
                if (slopeClicks == 1)
                {
                    slopeTool.Focus();
                    NodeSelectionTool.instance.enabled = true;
                }
                else
                {
                    slopeTool.Unfocus();
                    slopeClicks = 0;
                    NodeSelectionTool.instance.Reset();
                    NodeSelectionTool.instance.enabled = false;
                }
            };
            slopeToolSubmit.eventClicked += (component, click) =>
            {
                slopeTool.Unfocus();
                slopeToolSubmit.Unfocus();
                slopeClicks = 0;
                NodeSelectionTool.instance.Smooth();
                NodeSelectionTool.instance.Reset();
                NodeSelectionTool.instance.enabled = false;
            };

        }

        public override void OnLevelUnloading()
        {
            if (NodeSelectionTool.instance != null)
            {
                NodeSelectionTool.instance.enabled = false;
            }
        }
    }
}
