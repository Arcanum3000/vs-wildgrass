using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace Wildgrass
{
    public class ItemGrassSeeds : Item {
        public Block Plant { get; private set; }

        public override void OnLoaded(ICoreAPI api)
        {
            base.OnLoaded(api);
            Plant = api.World.GetBlock(AssetLocation.Create(Attributes?["seedFor"]?.AsString()));
        }

        public override void OnHeldInteractStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handling)
        {
            if(blockSel == null || byEntity.World == null || !byEntity.Controls.ShiftKey) {
                base.OnHeldInteractStart(slot, byEntity, blockSel, entitySel, firstEvent, ref handling);
                return;
            }

            if(Plant == null) {
                api.World.Logger.Debug($"Invalid block code in seedFor : {Attributes?["seedFor"]}");
                base.OnHeldInteractStart(slot, byEntity, blockSel, entitySel, firstEvent, ref handling);
                return;
            }

            IPlayer byPlayer = null;
            if(byEntity is EntityPlayer entityPlayer) byPlayer = byEntity.World.PlayerByUid(entityPlayer.PlayerUID);

            BlockSelection faceSel = blockSel.Clone();
            faceSel.Position.Add(blockSel.Face);

            string errcode = "";
            bool canPlace = Plant.TryPlaceBlock(byEntity.World, byPlayer, slot.Itemstack, faceSel, ref errcode);

            if(canPlace) {
                byEntity.World.PlaySoundAt(
                    Plant.Sounds.GetBreakSound(byPlayer),
                    faceSel.Position.X,
                    faceSel.Position.Y,
                    faceSel.Position.Z,
                    faceSel.Position.dimension);
                slot.TakeOut(1);
                slot.MarkDirty();
                handling = EnumHandHandling.PreventDefaultAction;
            }
        }

        public override WorldInteraction[] GetHeldInteractionHelp(ItemSlot inSlot)
        {
            return new WorldInteraction[] {
                new WorldInteraction() {
                    HotKeyCode = "shift",
                    ActionLangCode = "heldhelp-plant",
                    MouseButton = EnumMouseButton.Right
                }
            }.Append(base.GetHeldInteractionHelp(inSlot));
        }
    }
}