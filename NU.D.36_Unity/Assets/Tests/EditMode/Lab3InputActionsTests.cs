using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine.InputSystem;

namespace NU36.Tests
{
    /// <summary>Lab 3, Input System: the "Ucus" map and the preservation of the existing maps.</summary>
    public class Lab3InputActionsTests
    {
        const string AssetPath = "Assets/InputSystem_Actions.inputactions";
        InputActionAsset asset;

        [OneTimeSetUp]
        public void Load() => asset = AssetDatabase.LoadAssetAtPath<InputActionAsset>(AssetPath);

        [Test]
        public void UcusMap_HasTheFourAxisActions()
        {
            InputActionMap map = asset.FindActionMap("Ucus");
            Assert.IsNotNull(map, "Ucus map missing");
            CollectionAssert.AreEquivalent(new[] { "Yunuslama", "Yatis", "Sapma", "Gaz" }, map.actions.Select(a => a.name).ToArray());
            foreach (InputAction action in map.actions)
            {
                Assert.AreEqual(InputActionType.Value, action.type, action.name);
                Assert.AreEqual("Axis", action.expectedControlType, action.name);
            }
        }

        [TestCase("Yunuslama", "<Keyboard>/w", "<Keyboard>/s")]
        [TestCase("Yatis", "<Keyboard>/d", "<Keyboard>/a")]
        [TestCase("Sapma", "<Keyboard>/e", "<Keyboard>/q")]
        [TestCase("Gaz", "<Keyboard>/upArrow", "<Keyboard>/downArrow")]
        public void Action_Uses1DAxisCompositeWithWorksheetKeys(string actionName, string positive, string negative)
        {
            InputAction action = asset.FindActionMap("Ucus").FindAction(actionName);
            InputBinding[] bindings = action.bindings.ToArray();
            Assert.AreEqual(3, bindings.Length, "One composite with two parts expected");
            Assert.IsTrue(bindings[0].isComposite);
            StringAssert.AreEqualIgnoringCase("1DAxis", bindings[0].path);

            InputBinding pos = bindings.Single(b => b.isPartOfComposite && b.name == "Positive");
            InputBinding neg = bindings.Single(b => b.isPartOfComposite && b.name == "Negative");
            Assert.AreEqual(positive, pos.path);
            Assert.AreEqual(negative, neg.path);
            StringAssert.Contains("Keyboard&Mouse", pos.groups);
            StringAssert.Contains("Keyboard&Mouse", neg.groups);
        }

        [Test]
        public void ExistingMapsAndControlSchemes_ArePreserved()
        {
            InputActionMap player = asset.FindActionMap("Player");
            InputActionMap ui = asset.FindActionMap("UI");
            Assert.IsNotNull(player);
            Assert.IsNotNull(ui);
            Assert.AreEqual(9, player.actions.Count);
            Assert.AreEqual(35, player.bindings.Count);
            Assert.AreEqual(10, ui.actions.Count);
            Assert.AreEqual(38, ui.bindings.Count);
            CollectionAssert.AreEquivalent(new[] { "Keyboard&Mouse", "Gamepad", "Touch", "Joystick", "XR" },
                asset.controlSchemes.Select(s => s.name).ToArray());
        }
    }
}
