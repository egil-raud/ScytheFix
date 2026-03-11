using HarmonyLib;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using System.Collections.Generic;
using Vintagestory.API.Datastructures;
using Vintagestory.GameContent;
using System;

namespace MiniShipFix
{
    [HarmonyPatch]
    public class ItemScytheRadiusPatch
    {
        [HarmonyPatch(typeof(ItemShears), "GetNearblyMultibreakables")]
        [HarmonyPrefix]
        static bool PrefixGetNearblyMultibreakables(IWorldAccessor world, BlockPos pos, Vec3d hitPos, ref OrderedDictionary<BlockPos, float> __result, ItemShears __instance)
        {
            if (!(__instance is ItemScythe))
            {
                return true;
            }

            __result = GetCustomScytheRadius(world, pos, hitPos, __instance);
            return false;
        }

        private static OrderedDictionary<BlockPos, float> GetCustomScytheRadius(IWorldAccessor world, BlockPos centerPos, Vec3d hitPos, ItemShears shears)
        {
            var results = new OrderedDictionary<BlockPos, float>();

            // Ищем ближайшего игрока с косой в руках в радиусе 5 блоков
            IPlayer playerWithScythe = FindNearestPlayerWithScythe(world, centerPos);
            if (playerWithScythe == null || playerWithScythe.Entity == null)
            {
                return results;
            }

            float playerYaw = playerWithScythe.Entity.Pos.Yaw;
            Direction direction = GetPlayerDirection(playerYaw);
            var linePositions = GetHorizontalLinePositions(centerPos, direction);

            foreach (var targetPos in linePositions)
            {
                Block block = world.BlockAccessor.GetBlock(targetPos);
                bool canBreak = shears.CanMultiBreak(block);

                if (canBreak)
                {
                    float distance = (float)hitPos.SquareDistanceTo(
                        targetPos.X + 0.5,
                        targetPos.Y + 0.5,
                        targetPos.Z + 0.5
                    );

                    if (!results.ContainsKey(targetPos))
                    {
                        results.Add(targetPos, distance);
                    }
                }
            }

            return results;
        }

        // Ищет ближайшего игрока с косой в активной руке в радиусе 5 блоков
        private static IPlayer FindNearestPlayerWithScythe(IWorldAccessor world, BlockPos centerPos)
        {
            IPlayer nearestPlayer = null;
            double nearestDistance = double.MaxValue;

            // Ищем игроков только в радиусе 5 блоков
            var playersInRadius = world.GetPlayersAround(centerPos.ToVec3d(), 5, 5);

            foreach (var player in playersInRadius)
            {
                if (player?.Entity == null) continue;

                // Проверяем расстояние до игрока
                Vec3d playerPos = player.Entity.Pos.XYZ;
                Vec3d centerVec = new Vec3d(centerPos.X, centerPos.Y, centerPos.Z);
                double distance = playerPos.DistanceTo(centerVec);

                // Проверяем, держит ли игрок косу в активной руке
                ItemSlot activeSlot = player.InventoryManager?.ActiveHotbarSlot;
                if (activeSlot?.Itemstack?.Item is ItemScythe)
                {
                    if (distance < nearestDistance)
                    {
                        nearestDistance = distance;
                        nearestPlayer = player;
                    }
                }
            }

            return nearestPlayer;
        }

        private static List<BlockPos> GetHorizontalLinePositions(BlockPos centerPos, Direction direction)
        {
            var positions = new List<BlockPos>();

            // Горизонтальная линия из 5 блоков
            var lineOffsets = new[]
            {
                new int[] { 0, -2 },   // Левый
                new int[] { 0, -1 },   // Левый центральный
                new int[] { 0, 0 },    // Центральный
                new int[] { 0, 1 },    // Правый центральный
                new int[] { 0, 2 }     // Правый
            };

            foreach (var offset in lineOffsets)
            {
                int forward = offset[0];
                int side = offset[1];

                BlockPos pos = CalculatePosition(centerPos, direction, forward, side);
                positions.Add(pos);
            }

            return positions;
        }

        private static BlockPos CalculatePosition(BlockPos centerPos, Direction direction, int forward, int side)
        {
            return direction switch
            {
                Direction.North => new BlockPos(centerPos.X + side, centerPos.Y, centerPos.Z),
                Direction.South => new BlockPos(centerPos.X + side, centerPos.Y, centerPos.Z),
                Direction.East => new BlockPos(centerPos.X, centerPos.Y, centerPos.Z + side),
                Direction.West => new BlockPos(centerPos.X, centerPos.Y, centerPos.Z + side),
                _ => centerPos.Copy()
            };
        }

        private static Direction GetPlayerDirection(float yaw)
        {
            float normalizedYaw = yaw % GameMath.TWOPI;
            if (normalizedYaw < 0) normalizedYaw += GameMath.TWOPI;

            if (normalizedYaw >= GameMath.PI * 1.75f || normalizedYaw < GameMath.PI * 0.25f)
                return Direction.North;
            else if (normalizedYaw >= GameMath.PI * 0.25f && normalizedYaw < GameMath.PI * 0.75f)
                return Direction.East;
            else if (normalizedYaw >= GameMath.PI * 0.75f && normalizedYaw < GameMath.PI * 1.25f)
                return Direction.South;
            else
                return Direction.West;
        }

        private enum Direction
        {
            North,
            East,
            South,
            West
        }
    }

    public class ScytheFixModSystem : ModSystem
    {
        private Harmony harmony;

        public override void Start(ICoreAPI api)
        {
            base.Start(api);

            try
            {
                harmony = new Harmony("minishipfix.scytheradius");
                harmony.PatchAll();
            }
            catch (Exception e)
            {
                // Silent fail
            }
        }

        public override void Dispose()
        {
            harmony?.UnpatchAll("minishipfix.scytheradius");
            harmony = null;
        }
    }
}