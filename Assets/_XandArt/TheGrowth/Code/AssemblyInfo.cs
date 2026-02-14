using Sirenix.Serialization;

using XandArt.TheGrowth;
using XandArt.TheGrowth.Cards;
using XandArt.TheGrowth.Common;
using XandArt.TheGrowth.Crafting;
using XandArt.TheGrowth.Expedition;
using XandArt.TheGrowth.Expedition.Slots;
using XandArt.TheGrowth.Inventories;
using XandArt.TheGrowth.Logic;
using XandArt.TheGrowth.Slots;
using XandArt.TheGrowth.Story;

// Commands:
[assembly: BindTypeNameToType("XandArt.TheGrowth.Command", typeof(Command))]
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

// Conditions:
[assembly: BindTypeNameToType("XandArt.TheGrowth.Condition", typeof(Condition))]
[assembly: BindTypeNameToType("XandArt.TheGrowth.And", typeof(And))]
[assembly: BindTypeNameToType("XandArt.TheGrowth.CompareNumbers", typeof(CompareNumbers))]
[assembly: BindTypeNameToType("XandArt.TheGrowth.Not", typeof(Not))]
[assembly: BindTypeNameToType("XandArt.TheGrowth.Or", typeof(Or))]
[assembly: BindTypeNameToType("XandArt.TheGrowth.SlotContainsCard", typeof(SlotContainsCard))]
[assembly: BindTypeNameToType("XandArt.TheGrowth.SlotsAreEquals", typeof(SlotsAreEquals))]
[assembly: BindTypeNameToType("XandArt.TheGrowth.CardInGroups", typeof(CardInGroups))]
[assembly: BindTypeNameToType("XandArt.TheGrowth.ChargesCheck", typeof(ChargesCheck))]

// Evaluators:
[assembly: BindTypeNameToType("XandArt.TheGrowth.IEvaluator", typeof(IEvaluator))]
[assembly: BindTypeNameToType("XandArt.TheGrowth.Evaluator`1", typeof(Evaluator<>))]
[assembly: BindTypeNameToType("XandArt.TheGrowth.DealingSlots", typeof(DealingSlots))]
[assembly: BindTypeNameToType("XandArt.TheGrowth.FilterSlots", typeof(FilterSlots))]
[assembly: BindTypeNameToType("XandArt.TheGrowth.IteratingSlot", typeof(IteratingSlot))]
[assembly: BindTypeNameToType("XandArt.TheGrowth.NearestSlot", typeof(NearestSlot))]
[assembly: BindTypeNameToType("XandArt.TheGrowth.SlotFromCard", typeof(SlotFromCard))]
[assembly: BindTypeNameToType("XandArt.TheGrowth.SlotsFromCards", typeof(SlotsFromCards))]
[assembly: BindTypeNameToType("XandArt.TheGrowth.TableSlotsAll", typeof(TableSlotsAll))]
[assembly: BindTypeNameToType("XandArt.TheGrowth.TableSlotsAround", typeof(TableSlotsAround))]
[assembly: BindTypeNameToType("XandArt.TheGrowth.TableSlotsCombine", typeof(TableSlotsCombine))]
[assembly: BindTypeNameToType("XandArt.TheGrowth.TableSlotsFree", typeof(TableSlotsFree))]
[assembly: BindTypeNameToType("XandArt.TheGrowth.TableSlotsFromSide", typeof(TableSlotsFromSide))]
[assembly: BindTypeNameToType("XandArt.TheGrowth.DistanceEvaluator", typeof(DistanceEvaluator))]
[assembly: BindTypeNameToType("XandArt.TheGrowth.StepsEvaluator", typeof(StepsEvaluator))]
[assembly: BindTypeNameToType("XandArt.TheGrowth.PositionByCard", typeof(PositionByCard))]
[assembly: BindTypeNameToType("XandArt.TheGrowth.PositionBySlot", typeof(PositionBySlot))]
[assembly: BindTypeNameToType("XandArt.TheGrowth.PositionEvaluator", typeof(PositionEvaluator))]
[assembly: BindTypeNameToType("XandArt.TheGrowth.BoardSlot", typeof(BoardSlot))]
[assembly: BindTypeNameToType("XandArt.TheGrowth.Combine`1", typeof(Combine<>))]
[assembly: BindTypeNameToType("XandArt.TheGrowth.ConstantFloatEvaluator", typeof(ConstantFloatEvaluator))]
[assembly: BindTypeNameToType("XandArt.TheGrowth.First`1", typeof(First<>))]
[assembly: BindTypeNameToType("XandArt.TheGrowth.FloatExpressionEvaluator", typeof(FloatExpressionEvaluator))]
[assembly: BindTypeNameToType("XandArt.TheGrowth.Group`1", typeof(Group<>))]
[assembly: BindTypeNameToType("XandArt.TheGrowth.Random`1", typeof(Random<>))]
[assembly: BindTypeNameToType("XandArt.TheGrowth.ActiveCard", typeof(ActiveCard))]
[assembly: BindTypeNameToType("XandArt.TheGrowth.CardByPosition", typeof(CardByPosition))]
[assembly: BindTypeNameToType("XandArt.TheGrowth.CardsFromBoard", typeof(CardsFromBoard))]
[assembly: BindTypeNameToType("XandArt.TheGrowth.CardsFromSlots", typeof(CardsFromSlots))]
[assembly: BindTypeNameToType("XandArt.TheGrowth.CoveringCard", typeof(CoveringCard))]
[assembly: BindTypeNameToType("XandArt.TheGrowth.FilterCards", typeof(FilterCards))]
[assembly: BindTypeNameToType("XandArt.TheGrowth.IteratingCard", typeof(IteratingCard))]
[assembly: BindTypeNameToType("XandArt.TheGrowth.PlayerCard", typeof(PlayerCard))]
[assembly: BindTypeNameToType("XandArt.TheGrowth.RelativeCard", typeof(RelativeCard))]
[assembly: BindTypeNameToType("XandArt.TheGrowth.ChargesEvaluator", typeof(ChargesEvaluator))]