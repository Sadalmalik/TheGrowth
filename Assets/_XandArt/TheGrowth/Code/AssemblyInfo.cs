using Sirenix.Serialization;
using UnityEditor;

using XandArt.TheGrowth;
using XandArt.TheGrowth.Cards;
using XandArt.TheGrowth.Common;
using XandArt.TheGrowth.Crafting;
using XandArt.TheGrowth.Expedition;
using XandArt.TheGrowth.Inventories;
using XandArt.TheGrowth.Logic;
using XandArt.TheGrowth.Story;

[assembly: BindTypeNameToType("XandArt.TheGrowth.And", typeof(And))]
[assembly: BindTypeNameToType("XandArt.TheGrowth.CompareNumbers", typeof(CompareNumbers))]
[assembly: BindTypeNameToType("XandArt.TheGrowth.Not", typeof(Not))]
[assembly: BindTypeNameToType("XandArt.TheGrowth.Or", typeof(Or))]
[assembly: BindTypeNameToType("XandArt.TheGrowth.SetStoryStep", typeof(SetStoryStep))]
[assembly: BindTypeNameToType("XandArt.TheGrowth.StartDialogue", typeof(StartDialogue))]
[assembly: BindTypeNameToType("XandArt.TheGrowth.DestroyAllItems", typeof(DestroyAllItems))]
[assembly: BindTypeNameToType("XandArt.TheGrowth.MoveAllItems", typeof(MoveAllItems))]
[assembly: BindTypeNameToType("XandArt.TheGrowth.CallStep", typeof(CallStep))]
[assembly: BindTypeNameToType("XandArt.TheGrowth.EndGame", typeof(EndGame))]
[assembly: BindTypeNameToType("XandArt.TheGrowth.FlipCard", typeof(FlipCard))]
[assembly: BindTypeNameToType("XandArt.TheGrowth.MoveCamera", typeof(MoveCamera))]
[assembly: BindTypeNameToType("XandArt.TheGrowth.MoveCard", typeof(MoveCard))]
[assembly: BindTypeNameToType("XandArt.TheGrowth.SetExpeditionStep", typeof(SetExpeditionStep))]
[assembly: BindTypeNameToType("XandArt.TheGrowth.SlotContainsCard", typeof(SlotContainsCard))]
[assembly: BindTypeNameToType("XandArt.TheGrowth.SlotsAreEquals", typeof(SlotsAreEquals))]
[assembly: BindTypeNameToType("XandArt.TheGrowth.AddCrafts", typeof(AddCrafts))]
[assembly: BindTypeNameToType("XandArt.TheGrowth.RemoveCrafts", typeof(RemoveCrafts))]
[assembly: BindTypeNameToType("XandArt.TheGrowth.SetProjectCraft", typeof(SetProjectCraft))]
[assembly: BindTypeNameToType("XandArt.TheGrowth.Branch", typeof(Branch))]
[assembly: BindTypeNameToType("XandArt.TheGrowth.DebugLog", typeof(DebugLog))]
[assembly: BindTypeNameToType("XandArt.TheGrowth.ExecuteCommandConfig", typeof(ExecuteCommandConfig))]
[assembly: BindTypeNameToType("XandArt.TheGrowth.GoToHubRoom", typeof(GoToHubRoom))]
[assembly: BindTypeNameToType("XandArt.TheGrowth.GoToLocation", typeof(GoToLocation))]
[assembly: BindTypeNameToType("XandArt.TheGrowth.ShowUIElementsGroup", typeof(ShowUIElementsGroup))]
[assembly: BindTypeNameToType("XandArt.TheGrowth.ChangeCharges", typeof(ChangeCharges))]
[assembly: BindTypeNameToType("XandArt.TheGrowth.ForEachCard", typeof(ForEachCard))]
[assembly: BindTypeNameToType("XandArt.TheGrowth.ToggleChargesText", typeof(ToggleChargesText))]
[assembly: BindTypeNameToType("XandArt.TheGrowth.CardInGroups", typeof(CardInGroups))]
[assembly: BindTypeNameToType("XandArt.TheGrowth.ChargesCheck", typeof(ChargesCheck))]
