using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;

namespace ValheimDB.DataTypes;

public class PieceInfoWrapper
{
    [Flags]
    private enum PieceInfoFlags
    {
        None = 0,
        HasHealth = 1 << 0,
        HasDamageModifiers = 1 << 1,
        HasBuild = 1 << 2,
        HasName = 1 << 3,
        HasDescription = 1 << 4,
        HasContainer = 1 << 5,
        HasFireplace = 1 << 6,
        HasFermenter = 1 << 7,
        HasSmelter = 1 << 8,
        HasCraftingStation = 1 << 9
    }

    private static bool HasFlagFast(PieceInfoFlags value, PieceInfoFlags flag) => (value & flag) != 0;

    public class Container
    {
        public int Width;
        public int Height;
    }

    public class Fireplace
    {
        public int StartFuel;
        public int MaxFuel;
        public int SecPerFuel;
        public string FuelItem;
    }

    public class Conversion
    {
        public string From;
        public string To;
        public int Amount;
    }

    public class Fermenter
    {
        public int Duration;
        public List<Conversion> Conversions;
    }

    public class Smelter
    {
        public string FuelItem;
        public int MaxOre;
        public int MaxFuel;
        public int FuelPerProduct;
        public int SecPerProduct;
        public List<Conversion> Conversions;
    }

    public class PieceInfo : ISerializableParameter
    {
        public int? Health;
        [CanBeNull] public HitData.DamageModifiers? DamageModifiers;
        [CanBeNull] public List<string> Build;
        [CanBeNull] public Container Container;
        [CanBeNull] public Fireplace Fireplace;
        [CanBeNull] public Fermenter Fermenter;
        [CanBeNull] public Smelter Smelter;
        [CanBeNull] public string Name;
        [CanBeNull] public string Description;
        [CanBeNull] public string CraftingStation;

        public void Serialize(ref ZPackage pkg)
        {
            PieceInfoFlags flags = PieceInfoFlags.None;

            if (Health.HasValue) flags |= PieceInfoFlags.HasHealth;
            if (DamageModifiers.HasValue) flags |= PieceInfoFlags.HasDamageModifiers;
            if (Build != null) flags |= PieceInfoFlags.HasBuild;
            if (!string.IsNullOrEmpty(Name)) flags |= PieceInfoFlags.HasName;
            if (!string.IsNullOrEmpty(Description)) flags |= PieceInfoFlags.HasDescription;
            if (Container != null) flags |= PieceInfoFlags.HasContainer;
            if (Fireplace != null) flags |= PieceInfoFlags.HasFireplace;
            if (Fermenter != null) flags |= PieceInfoFlags.HasFermenter;
            if (Smelter != null) flags |= PieceInfoFlags.HasSmelter;
            if (!string.IsNullOrEmpty(CraftingStation)) flags |= PieceInfoFlags.HasCraftingStation;

            pkg.Write((int)flags);

            if (HasFlagFast(flags, PieceInfoFlags.HasHealth)) pkg.Write(Health.Value);
            if (HasFlagFast(flags, PieceInfoFlags.HasDamageModifiers))
            {
                var dmg = DamageModifiers.Value;
                pkg.Write((int)dmg.m_blunt);
                pkg.Write((int)dmg.m_slash);
                pkg.Write((int)dmg.m_pierce);
                pkg.Write((int)dmg.m_chop);
                pkg.Write((int)dmg.m_pickaxe);
                pkg.Write((int)dmg.m_fire);
                pkg.Write((int)dmg.m_frost);
                pkg.Write((int)dmg.m_lightning);
                pkg.Write((int)dmg.m_poison);
                pkg.Write((int)dmg.m_spirit);
            }

            if (HasFlagFast(flags, PieceInfoFlags.HasBuild))
            {
                pkg.Write(Build.Count);
                for (int i = 0; i < Build.Count; ++i) pkg.Write(Build[i]);
            }

            if (HasFlagFast(flags, PieceInfoFlags.HasName)) pkg.Write(Name);
            if (HasFlagFast(flags, PieceInfoFlags.HasDescription)) pkg.Write(Description);

            if (HasFlagFast(flags, PieceInfoFlags.HasContainer))
            {
                pkg.Write(Container.Width);
                pkg.Write(Container.Height);
            }

            if (HasFlagFast(flags, PieceInfoFlags.HasFireplace))
            {
                pkg.Write(Fireplace.StartFuel);
                pkg.Write(Fireplace.MaxFuel);
                pkg.Write(Fireplace.SecPerFuel);
                pkg.Write(Fireplace.FuelItem);
            }

            if (HasFlagFast(flags, PieceInfoFlags.HasFermenter))
            {
                pkg.Write(Fermenter.Duration);
                pkg.Write(Fermenter.Conversions != null);
                if (Fermenter.Conversions != null)
                {
                    pkg.Write(Fermenter.Conversions.Count);
                    for (int i = 0; i < Fermenter.Conversions.Count; ++i)
                    {
                        var conv = Fermenter.Conversions[i];
                        pkg.Write(conv.From);
                        pkg.Write(conv.To);
                        pkg.Write(conv.Amount);
                    }
                }
            }

            if (HasFlagFast(flags, PieceInfoFlags.HasSmelter))
            {
                pkg.Write(!string.IsNullOrEmpty(Smelter.FuelItem));
                if (!string.IsNullOrEmpty(Smelter.FuelItem)) pkg.Write(Smelter.FuelItem);
                pkg.Write(Smelter.MaxOre);
                pkg.Write(Smelter.MaxFuel);
                pkg.Write(Smelter.FuelPerProduct);
                pkg.Write(Smelter.SecPerProduct);
                pkg.Write(Smelter.Conversions != null);
                if (Smelter.Conversions != null)
                {
                    pkg.Write(Smelter.Conversions.Count);
                    for (int i = 0; i < Smelter.Conversions.Count; ++i)
                    {
                        var conv = Smelter.Conversions[i];
                        pkg.Write(conv.From);
                        pkg.Write(conv.To);
                        pkg.Write(conv.Amount);
                    }
                }
            }

            if (HasFlagFast(flags, PieceInfoFlags.HasCraftingStation)) pkg.Write(CraftingStation);
        }

        public void Deserialize(ref ZPackage pkg)
        {
            PieceInfoFlags flags = (PieceInfoFlags)pkg.ReadInt();

            if (HasFlagFast(flags, PieceInfoFlags.HasHealth)) Health = pkg.ReadInt();
            if (HasFlagFast(flags, PieceInfoFlags.HasDamageModifiers))
                DamageModifiers = new HitData.DamageModifiers
                {
                    m_blunt = (HitData.DamageModifier)pkg.ReadInt(),
                    m_slash = (HitData.DamageModifier)pkg.ReadInt(),
                    m_pierce = (HitData.DamageModifier)pkg.ReadInt(),
                    m_chop = (HitData.DamageModifier)pkg.ReadInt(),
                    m_pickaxe = (HitData.DamageModifier)pkg.ReadInt(),
                    m_fire = (HitData.DamageModifier)pkg.ReadInt(),
                    m_frost = (HitData.DamageModifier)pkg.ReadInt(),
                    m_lightning = (HitData.DamageModifier)pkg.ReadInt(),
                    m_poison = (HitData.DamageModifier)pkg.ReadInt(),
                    m_spirit = (HitData.DamageModifier)pkg.ReadInt()
                };

            if (HasFlagFast(flags, PieceInfoFlags.HasBuild))
            {
                int count = pkg.ReadInt();
                Build = new List<string>(count);
                for (int i = 0; i < count; ++i) Build.Add(pkg.ReadString());
            }

            if (HasFlagFast(flags, PieceInfoFlags.HasName)) Name = pkg.ReadString();
            if (HasFlagFast(flags, PieceInfoFlags.HasDescription)) Description = pkg.ReadString();
            if (HasFlagFast(flags, PieceInfoFlags.HasContainer))
                Container = new Container { Width = pkg.ReadInt(), Height = pkg.ReadInt() };
            if (HasFlagFast(flags, PieceInfoFlags.HasFireplace))
                Fireplace = new Fireplace
                {
                    StartFuel = pkg.ReadInt(),
                    MaxFuel = pkg.ReadInt(),
                    SecPerFuel = pkg.ReadInt(),
                    FuelItem = pkg.ReadString()
                };
            if (HasFlagFast(flags, PieceInfoFlags.HasFermenter))
            {
                Fermenter = new Fermenter { Duration = pkg.ReadInt() };
                if (pkg.ReadBool())
                {
                    int count = pkg.ReadInt();
                    Fermenter.Conversions = new List<Conversion>(count);
                    for (int i = 0; i < count; ++i)
                        Fermenter.Conversions.Add(new Conversion
                        {
                            From = pkg.ReadString(),
                            To = pkg.ReadString(),
                            Amount = pkg.ReadInt()
                        });
                }
            }

            if (HasFlagFast(flags, PieceInfoFlags.HasSmelter))
            {
                Smelter = new Smelter();
                if (pkg.ReadBool()) Smelter.FuelItem = pkg.ReadString();
                Smelter.MaxOre = pkg.ReadInt();
                Smelter.MaxFuel = pkg.ReadInt();
                Smelter.FuelPerProduct = pkg.ReadInt();
                Smelter.SecPerProduct = pkg.ReadInt();
                if (pkg.ReadBool())
                {
                    int count = pkg.ReadInt();
                    Smelter.Conversions = new List<Conversion>(count);
                    for (int i = 0; i < count; ++i)
                        Smelter.Conversions.Add(new Conversion
                        {
                            From = pkg.ReadString(),
                            To = pkg.ReadString(),
                            Amount = pkg.ReadInt()
                        });
                }
            }

            if (HasFlagFast(flags, PieceInfoFlags.HasCraftingStation)) CraftingStation = pkg.ReadString();
        }

        public static implicit operator PieceInfo(Piece p)
        {
            PieceInfo newInfo = new PieceInfo();
            newInfo.Name = p.m_name;
            newInfo.Description = p.m_description;
            if (p.GetComponent<WearNTear>() is { } wnt)
            {
                newInfo.Health = (int)wnt.m_health;
                newInfo.DamageModifiers = wnt.m_damages.Clone();
            }

            if (p.GetComponent<global::Container>() is { } container) newInfo.Container = new Container { Width = container.m_width, Height = container.m_height };
            if (p.GetComponent<global::Fireplace>() is { } fireplace) newInfo.Fireplace = new Fireplace { StartFuel = (int)fireplace.m_startFuel, MaxFuel = (int)fireplace.m_maxFuel, SecPerFuel = (int)fireplace.m_secPerFuel, FuelItem = fireplace.m_fuelItem.name };
            if (p.GetComponent<global::Fermenter>() is { } fermenter)
            {
                newInfo.Fermenter = new Fermenter { Duration = (int)fermenter.m_fermentationDuration };
                newInfo.Fermenter.Conversions = [];
                for (int i = 0; i < fermenter.m_conversion.Count; ++i)
                {
                    global::Fermenter.ItemConversion conv = fermenter.m_conversion[i];
                    newInfo.Fermenter.Conversions.Add(new Conversion { From = conv.m_from.name, To = conv.m_to.name, Amount = (int)conv.m_producedItems });
                }
            }

            if (p.GetComponent<global::Smelter>() is { } smelter)
            {
                newInfo.Smelter = new Smelter
                {
                    FuelItem = smelter.m_fuelItem.name,
                    MaxOre = (int)smelter.m_maxOre,
                    MaxFuel = (int)smelter.m_maxFuel,
                    FuelPerProduct = (int)smelter.m_fuelPerProduct,
                    SecPerProduct = (int)smelter.m_secPerProduct
                };
                newInfo.Smelter.Conversions = [];
                for (int i = 0; i < smelter.m_conversion.Count; ++i)
                {
                    global::Smelter.ItemConversion conv = smelter.m_conversion[i];
                    newInfo.Smelter.Conversions.Add(new Conversion { From = conv.m_from.name, To = conv.m_to.name, Amount = 1 });
                }
            }

            if (p.m_resources != null && p.m_resources.Length > 0)
            {
                newInfo.Build = [];
                for (int i = 0; i < p.m_resources.Length; ++i)
                {
                    Piece.Requirement req = p.m_resources[i];
                    newInfo.Build.Add($"{req.m_resItem.gameObject.name}:{req.m_amount}:{req.m_recover}");
                }
            }

            if (p.m_craftingStation) newInfo.CraftingStation = p.m_craftingStation.name;
            return newInfo;
        }

        public void Apply(ZNetScene zns, Piece p)
        {
            if (!string.IsNullOrEmpty(Name)) p.m_name = Name;
            if (!string.IsNullOrEmpty(Description)) p.m_description = Description;
            if (!string.IsNullOrEmpty(CraftingStation)) p.m_craftingStation = zns.GetPrefab(CraftingStation).GetComponent<CraftingStation>();
            if (p.GetComponent<WearNTear>() is { } wnt)
            {
                if (Health.HasValue) wnt.m_health = Health.Value;
                if (DamageModifiers != null) wnt.m_damages = DamageModifiers.Value;
            }

            if (p.GetComponent<global::Container>() is { } container && Container != null)
            {
                container.m_width = Container.Width;
                container.m_height = Container.Height;
            }

            if (p.GetComponent<global::Fireplace>() is { } fireplace && Fireplace != null)
            {
                fireplace.m_startFuel = Fireplace.StartFuel;
                fireplace.m_maxFuel = Fireplace.MaxFuel;
                fireplace.m_secPerFuel = Fireplace.SecPerFuel;
                fireplace.m_fuelItem = zns.GetPrefab(Fireplace.FuelItem).GetComponent<ItemDrop>();
            }

            if (p.GetComponent<global::Fermenter>() is { } fermenter && Fermenter != null)
            {
                fermenter.m_fermentationDuration = Fermenter.Duration;
                if (Fermenter.Conversions != null)
                {
                    List<global::Fermenter.ItemConversion> conversions = [];
                    for (int i = 0; i < Fermenter.Conversions.Count; ++i)
                    {
                        Conversion conv = Fermenter.Conversions[i];
                        conversions.Add(new global::Fermenter.ItemConversion
                        {
                            m_from = zns.GetPrefab(conv.From).GetComponent<ItemDrop>(),
                            m_to = zns.GetPrefab(conv.To).GetComponent<ItemDrop>(),
                            m_producedItems = conv.Amount
                        });
                    }

                    fermenter.m_conversion = conversions;
                }
            }

            if (p.GetComponent<global::Smelter>() is { } smelter && Smelter != null)
            {
                smelter.m_fuelItem = zns.GetPrefab(Smelter.FuelItem).GetComponent<ItemDrop>();
                smelter.m_maxOre = Smelter.MaxOre;
                smelter.m_maxFuel = Smelter.MaxFuel;
                smelter.m_fuelPerProduct = Smelter.FuelPerProduct;
                smelter.m_secPerProduct = Smelter.SecPerProduct;
                if (Smelter.Conversions != null)
                {
                    List<global::Smelter.ItemConversion> conversions = [];
                    for (int i = 0; i < Smelter.Conversions.Count; ++i)
                    {
                        Conversion conv = Smelter.Conversions[i];
                        conversions.Add(new global::Smelter.ItemConversion
                        {
                            m_from = zns.GetPrefab(conv.From).GetComponent<ItemDrop>(),
                            m_to = zns.GetPrefab(conv.To).GetComponent<ItemDrop>()
                        });
                    }

                    smelter.m_conversion = conversions.ToList();
                }
            }

            if (Build != null)
            {
                List<Piece.Requirement> requirements = [];
                for (int i = 0; i < Build.Count; ++i)
                {
                    string req = Build[i];
                    string[] split = req.Split(':');
                    requirements.Add(new Piece.Requirement
                    {
                        m_resItem = zns.GetPrefab(split[0]).GetComponent<ItemDrop>(),
                        m_amount = int.Parse(split[1]),
                        m_recover = split.Length > 2 && bool.Parse(split[2])
                    });
                }

                p.m_resources = requirements.ToArray();
            }
        }
    }
}