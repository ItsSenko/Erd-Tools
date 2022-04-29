﻿using Keystone;
using PropertyHook;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using Category = Erd_Tools.ERItem.Category;
using static SoulsFormats.PARAMDEF;
using System.Collections;
using System.Text.RegularExpressions;
using SoulsFormats;

namespace Erd_Tools
{
    public class ERHook : PHook, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public event EventHandler<PHEventArgs>? OnSetup;
        private void RaiseOnSetup()
        {
            OnSetup?.Invoke(this, new PHEventArgs(this));
        }

        private PHPointer GameDataMan { get; set; }
        private PHPointer PlayerGameData { get; set; }
        private PHPointer PlayerInventory { get; set; }
        private PHPointer SoloParamRepository { get; set; }
        private PHPointer CapParamCall { get; set; }
        public PHPointer ItemGive { get; set; }
        public PHPointer MapItemMan { get; set; }
        public PHPointer EventFlagMan { get; set; }
        public PHPointer SetEventFlagFunction { get; set; }
        public PHPointer WorldChrMan { get; set; }
        public PHPointer PlayerIns { get; set; }
        public PHPointer DisableOpenMap { get; set; }
        public PHPointer CombatCloseMap { get; set; }
        public PHPointer WorldAreaWeather { get; set; }
        public static bool Reading { get; set; }
        public string ID => Process?.Id.ToString() ?? "Not Hooked";
        public List<PHPointer>? ParamPointers { get; set; }
        //private PHPointer DurabilityAddr { get; set; }
        //private PHPointer DurabilitySpecialAddr { get; set; }
        public bool Loaded => PlayerIns != null ? PlayerIns.Resolve() != IntPtr.Zero : false;
        public bool Setup = false;
        public bool Focused => Hooked && User32.GetForegroundProcessID() == Process.Id;

        public ERHook(int refreshInterval, int minLifetime, Func<Process, bool> processSelector)
            : base(refreshInterval, minLifetime, processSelector)
        {
            OnHooked += ERHook_OnHooked;
            OnUnhooked += ERHook_OnUnhooked;

            GameDataMan = RegisterRelativeAOB(EROffsets.GameDataManAoB, EROffsets.RelativePtrAddressOffset, EROffsets.RelativePtrInstructionSize, 0x0);
            PlayerGameData = CreateChildPointer(GameDataMan, EROffsets.PlayerGameData);
            PlayerInventory = CreateChildPointer(PlayerGameData, EROffsets.EquipInventoryDataOffset, EROffsets.PlayerInventoryOffset);

            SoloParamRepository = RegisterRelativeAOB(EROffsets.SoloParamRepositoryAoB, EROffsets.RelativePtrAddressOffset, EROffsets.RelativePtrInstructionSize, 0x0);

            ItemGive = RegisterAbsoluteAOB(EROffsets.ItemGiveAoB);
            MapItemMan = RegisterRelativeAOB(EROffsets.MapItemManAoB, EROffsets.RelativePtrAddressOffset, EROffsets.RelativePtrInstructionSize);
            EventFlagMan = RegisterRelativeAOB(EROffsets.EventFlagManAoB, EROffsets.RelativePtrAddressOffset, EROffsets.RelativePtrInstructionSize, 0x0);
            SetEventFlagFunction = RegisterAbsoluteAOB(EROffsets.EventCallAoB);

            CapParamCall = RegisterAbsoluteAOB(EROffsets.CapParamCallAoB);

            WorldChrMan = RegisterRelativeAOB(EROffsets.WorldChrManAoB, EROffsets.RelativePtrAddressOffset, EROffsets.RelativePtrInstructionSize, 0x0);
            PlayerIns = CreateChildPointer(WorldChrMan, EROffsets.PlayerInsOffset);

            DisableOpenMap = RegisterAbsoluteAOB(EROffsets.DisableOpenMapAoB);
            CombatCloseMap = RegisterAbsoluteAOB(EROffsets.CombatCloseMapAoB);
            WorldAreaWeather = RegisterRelativeAOB(EROffsets.WorldAreaWeatherAoB, EROffsets.RelativePtrAddressOffset, EROffsets.RelativePtrInstructionSize, 0x0);

            ItemEventDictionary = BuildItemEventDictionary();
            ERItemCategory.GetItemCategories();

        }

        private void ERHook_OnUnhooked(object? sender, PHEventArgs e)
        {
            Setup = false;
        }

        private void ERHook_OnHooked(object? sender, PHEventArgs e)
        {
            //IntPtr gameDataMan = GameDataMan.Resolve();
            //IntPtr paramss = SoloParamRepository.Resolve();
            //IntPtr itemGive = ItemGive.Resolve();
            //IntPtr mapItemMan = MapItemMan.Resolve();
            //IntPtr eventFlagMan = EventFlagMan.Resolve();
            //IntPtr setEventFlagFunction = SetEventFlagFunction.Resolve();
            //IntPtr capParamCall = CapParamCall.Resolve();
            //IntPtr worldChrMan = WorldChrMan.Resolve();

            //IntPtr disableOpenMap = DisableOpenMap.Resolve();
            //IntPtr combatCloseMap = CombatCloseMap.Resolve();

            Params = GetParams();
            ReadParams();
            RaiseOnSetup();
            Setup = true;

            //LogABunchOfStuff();
        }

        private void LogABunchOfStuff()
        {
            List<string> list = new List<string>();
            list.Add($"WorldChrMan {WorldChrMan.Resolve():X2}");
            list.Add($"ItemGib {ItemGive.Resolve():X2}");
            list.Add($"GameDataMan {GameDataMan.Resolve():X2}");
            list.Add($"SoloParamRepository {SoloParamRepository.Resolve():X2}");
            File.WriteAllLines(Environment.CurrentDirectory + @"\HookLog.txt", list);
        }

        public void Update()
        {
            OnPropertyChanged(nameof(Setup));
            OnPropertyChanged(nameof(ID));

            if (!Setup)
                return;

            OnPropertyChanged(nameof(Loaded));
            OnPropertyChanged(nameof(InventoryCount));
            OnPropertyChanged(nameof(TargetEnemyHandle));
            OnPropertyChanged(nameof(TargetHp));
            OnPropertyChanged(nameof(TargetHpMax));
            OnPropertyChanged(nameof(TargetFp));
            OnPropertyChanged(nameof(TargetFpMax));
            OnPropertyChanged(nameof(TargetStam));
            OnPropertyChanged(nameof(TargetStamMax));
            OnPropertyChanged(nameof(TargetPoison));
            OnPropertyChanged(nameof(TargetPoisonMax));
            OnPropertyChanged(nameof(TargetRot));
            OnPropertyChanged(nameof(TargetRotMax));
            OnPropertyChanged(nameof(TargetBleed));
            OnPropertyChanged(nameof(TargetBleedMax));
            OnPropertyChanged(nameof(TargetBlight));
            OnPropertyChanged(nameof(TargetBlightMax));
            OnPropertyChanged(nameof(TargetFrost));
            OnPropertyChanged(nameof(TargetFrostMax));
            OnPropertyChanged(nameof(TargetSleep));
            OnPropertyChanged(nameof(TargetSleepMax));
            OnPropertyChanged(nameof(TargetMadness));
            OnPropertyChanged(nameof(TargetMadnessMax));
            OnPropertyChanged(nameof(TargetStagger));
            OnPropertyChanged(nameof(TargetStaggerMax));
            OnPropertyChanged(nameof(TargetResetTime));
            OnPropertyChanged(nameof(TargetChrType));
            OnPropertyChanged(nameof(TargetEnemyInsPtr));
        }

        public ERParam? EquipParamAccessory;
        public ERParam? EquipParamGem;
        public ERParam? EquipParamGoods;
        public ERParam? EquipParamProtector;
        public ERParam? EquipParamWeapon;
        public ERParam? MagicParam;
        public ERParam? NpcParam;

        private Engine Engine = new Engine(Architecture.X86, Mode.X64);
        //TKCode
        private void AsmExecute(string asm)
        {
            //Assemble once to get the size
            EncodedData? bytes = Engine.Assemble(asm, (ulong)Process.MainModule.BaseAddress);
            //DebugPrintArray(bytes.Buffer);
            KeystoneError error = Engine.GetLastKeystoneError();
            if (error != KeystoneError.KS_ERR_OK)
                throw new Exception("Something went wrong during assembly. Code could not be assembled.");

            IntPtr insertPtr = GetPrefferedIntPtr(bytes.Buffer.Length, Kernel32.PAGE_EXECUTE_READWRITE);

            //Reassemble with the location of the isertPtr to support relative instructions
            bytes = Engine.Assemble(asm, (ulong)insertPtr);
            error = Engine.GetLastKeystoneError();

            Kernel32.WriteBytes(Handle, insertPtr, bytes.Buffer);
#if DEBUG
            DebugPrintArray(bytes.Buffer);
#endif

            Execute(insertPtr);
            Free(insertPtr);
        }

#if DEBUG
        private static void DebugPrintArray(byte[] bytes)
        {
            Debug.WriteLine("");
            foreach (byte b in bytes)
            {
                Debug.Write($"{b.ToString("X2")} ");
            }
            Debug.WriteLine("");
        }
#endif

        #region Params

        public List<ERParam> Params;

        private List<ERParam> GetParams()
        {
            List<ERParam> paramList = new List<ERParam>();
            string paramPath = $"{Util.ExeDir}/Resources/Params/";

            string pointerPath = $"{paramPath}/Pointers/";
            string[] paramPointers = Directory.GetFiles(pointerPath, "*.txt");
            foreach (string path in paramPointers)
            {
                string[] pointers = File.ReadAllLines(path);
                AddParam(paramList, paramPath, path, pointers);
            }

            return paramList;
        }

        public void AddParam(List<ERParam> paramList, string paramPath, string path, string[] pointers)
        {
            foreach (string entry in pointers)
            {
                if (!Util.IsValidTxtResource(entry))
                    continue;

                string[] info = entry.TrimComment().Split(':');
                string name = info[1];
                string defName = info.Length > 2 ? info[2] : name;

                string defPath = $"{paramPath}/Defs/{defName}.xml";
                if (!File.Exists(defPath))
                    throw new Exception($"The PARAMDEF {defName} does not exist for {entry}. If the PARAMDEF is named differently than the param name, add another \":\" and append the PARAMDEF name" +
                        $"Example: 3130:WwiseValueToStrParam_BgmBossChrIdConv:WwiseValueToStrConvertParamFormat");

                int offset = int.Parse(info[0], System.Globalization.NumberStyles.HexNumber);

                PHPointer pointer = GetParamPointer(offset);

                PARAMDEF paramDef = XmlDeserialize(defPath);

                ERParam param = new ERParam(pointer, offset, paramDef, name);

                SetParamPtrs(param);

                paramList.Add(param);
            }
            paramList.Sort();
        }

        private void SetParamPtrs(ERParam param)
        {
            switch (param.Name)
            {
                case "EquipParamAccessory":
                    EquipParamAccessory = param;
                    break;
                case "EquipParamGem":
                    EquipParamGem = param;
                    break;
                case "EquipParamGoods":
                    EquipParamGoods = param;
                    break;
                case "EquipParamProtector":
                    EquipParamProtector = param;
                    break;
                case "EquipParamWeapon":
                    EquipParamWeapon = param;
                    break;
                case "Magic":
                    MagicParam = param;
                    break;
                case "NpcParam":
                    NpcParam = param;
                    break;
                default:
                    break;
            }
        }

        internal PHPointer GetParamPointer(int offset)
        {
            return CreateChildPointer(SoloParamRepository, new int[] { offset, 0x80, 0x80 });
        }
        public void SaveParam(ERParam param)
        {
            string asmString = Util.GetEmbededResource("Assembly.SaveParams.asm");
            string asm = string.Format(asmString, SoloParamRepository.Resolve(), param.Offset, CapParamCall.Resolve());
            AsmExecute(asm);
        }
        public void RestoreParams()
        {
            if (!Setup)
                return;

            EquipParamWeapon.RestoreParam();
            EquipParamGem.RestoreParam();
        }


        #endregion

        public void SetEventFlag(int flag)
        {
            IntPtr idPointer = GetPrefferedIntPtr(sizeof(int));
            Kernel32.WriteInt32(Handle, idPointer, flag);

            string asmString = Util.GetEmbededResource("Assembly.SetEventFlag.asm");
            string asm = string.Format(asmString, EventFlagMan.Resolve(), idPointer.ToString("X2"), SetEventFlagFunction.Resolve());
            AsmExecute(asm);
            Free(idPointer);
        }

        #region Inventory

        private static Regex ItemEventEntryRx = new Regex(@"^(?<event>\S+) (?<item>\S+)$", RegexOptions.CultureInvariant);

        private static Dictionary<int, int> ItemEventDictionary;

        private Dictionary<int, int> BuildItemEventDictionary()
        {
            Dictionary<int, int> itemEventDictionary = new Dictionary<int, int>();
            string[] goodsEvents = Util.GetListResource("Resources/Events/GoodsEvents.txt");
            foreach (string line in goodsEvents)
            {
                if (!Util.IsValidTxtResource(line))
                    continue;

                Match itemEntry = ItemEventEntryRx.Match(line.TrimComment());
                int eventID = Convert.ToInt32(itemEntry.Groups["event"].Value);
                int itemID = Convert.ToInt32(itemEntry.Groups["item"].Value);
                itemEventDictionary.Add(itemID + (int)Category.Goods, eventID);
            }

            return itemEventDictionary;
        }
        private void ReadParams()
        {
            foreach (ERItemCategory category in ERItemCategory.All)
            {
                foreach (ERItem item in category.Items)
                {
                    SetupItem(item);
                    int fullID = item.ID + (int)Category.Goods;
                    item.EventID = ItemEventDictionary.ContainsKey(fullID) ? ItemEventDictionary[fullID] : -1;
                }
            }

            foreach (ERItemCategory category in ERItemCategory.All)
            {
                if (category.Category == Category.Weapons)
                    foreach (ERWeapon weapon in category.Items)
                    {
                        ERGem gem = ERGem.All.FirstOrDefault(gem => gem.SwordArtID == weapon.SwordArtId);
                        if (gem != null)
                            weapon.DefaultGem = gem;
                    }
            }
        }

        private void SetupItem(ERItem item)
        {
            switch (item.ItemCategory)
            {
                case Category.Weapons:
                    item.SetupItem(EquipParamWeapon);
                    break;
                case Category.Protector:
                    item.SetupItem(EquipParamProtector);
                    break;
                case Category.Accessory:
                    item.SetupItem(EquipParamAccessory);
                    break;
                case Category.Goods:
                    item.SetupItem(EquipParamGoods);
                    break;
                case Category.Gem:
                    item.SetupItem(EquipParamGem);
                    break;
                default:
                    break;
            }
        }

        public void GetItem(int id, int quantity, int infusion, int upgrade, int gem)
        {
            byte[]   itemInfobytes = new byte[0x34];
            IntPtr itemInfo = GetPrefferedIntPtr(0x34);

            byte[] bytes = BitConverter.GetBytes(0x1);
            Array.Copy(bytes, 0x0, itemInfobytes, (int)EROffsets.ItemGiveStruct.Count, bytes.Length);

            bytes = BitConverter.GetBytes(id + infusion + upgrade);
            Array.Copy(bytes, 0x0, itemInfobytes, (int)EROffsets.ItemGiveStruct.ID, bytes.Length);

            bytes = BitConverter.GetBytes(quantity);
            Array.Copy(bytes, 0x0, itemInfobytes, (int)EROffsets.ItemGiveStruct.Quantity, bytes.Length);

            bytes = BitConverter.GetBytes(gem);
            Array.Copy(bytes, 0x0, itemInfobytes, (int)EROffsets.ItemGiveStruct.Gem, bytes.Length);

            Kernel32.WriteBytes(Handle, itemInfo, itemInfobytes);

            string asmString = Util.GetEmbededResource("Assembly.ItemGive.asm");
            string asm = string.Format(asmString, itemInfo.ToString("X2"), MapItemMan.Resolve(), ItemGive.Resolve() + EROffsets.ItemGiveOffset);
            AsmExecute(asm);
            Free(itemInfo);
        }

        List<ERInventoryEntry>? Inventory;
        public int InventoryCount => PlayerGameData.ReadInt32((int)EROffsets.PlayerGameDataStruct.InventoryCount);
        public int LastInventoryCount { get; set; }

        public IEnumerable GetInventory()
        {
            if (InventoryCount != LastInventoryCount)
                GetInventoryList();

            return Inventory;
        }
        private void GetInventoryList()
        {
            Inventory = new List<ERInventoryEntry>();
            LastInventoryCount = InventoryCount;

            byte[] bytes = PlayerInventory.ReadBytes(0x0, (uint)InventoryCount * EROffsets.PlayerInventoryEntrySize);

            for (int i = 0; i < InventoryCount; i++)
            {
                byte[] entry = new byte[EROffsets.PlayerInventoryEntrySize];
                Array.Copy(bytes, i * EROffsets.PlayerInventoryEntrySize, entry, 0, entry.Length);
                Inventory.Add(new ERInventoryEntry(entry, this));
            }
        }

        public void ResetInventory()
        {
            Inventory = new List<ERInventoryEntry>();
            LastInventoryCount = 0;
        }
        #endregion

        #region Target  

        public enum PhantomParam
        {
            Normal = 0x00,
            Friendly = 0x01,
            Invader = 0x02,
            Phantom = 0x03,
            Original = 0x05,
            HostOfFingers = 0x08,
            InvaderNoText = 0x10,
            BrightWhitePhantomNoText = 0x14,
            BloodyFingerRedText = 0x15,
            RecusantText = 0x16,
            BlueHunterText = 0x17,
            BloodyFingerRedText2 = 0x18,
            OrangeGlowNoText = 0x19,
            InvalidInvader = 0x20,
            InvalidInvader1 = 0x21,
            InvalidInvader2 = 0x22,
        }


        private int CurrentTargetHandle => PlayerIns?.ReadInt32((int)EROffsets.PlayerIns.TargetHandle) ?? 0;
        private int CurrentTargetArea => PlayerIns?.ReadInt32((int)EROffsets.PlayerIns.TargetArea) ?? 0;
        private PHPointer? _targetEnemyIns { get; set; }
        private PHPointer? TargetEnemyIns
        {
            get => _targetEnemyIns;
            set
            {
                _targetEnemyIns = value;
                TargetEnemyModuleBase = _targetEnemyIns != null ? CreateChildPointer(_targetEnemyIns, (int)EROffsets.EnemyIns.ModuleBase) : null;
                TargetEnemyData = _targetEnemyIns != null ? CreateChildPointer(TargetEnemyModuleBase, (int)EROffsets.ModuleBase.EnemyData) : null;
                TargetEnemyResistance = _targetEnemyIns != null ? CreateChildPointer(TargetEnemyModuleBase, (int)EROffsets.ModuleBase.ResistenceData) : null;
                TargetEnemyStagger = _targetEnemyIns != null ? CreateChildPointer(TargetEnemyModuleBase, (int)EROffsets.ModuleBase.StaggerData) : null;
            }
        }
        public string TargetEnemyInsPtr => _targetEnemyIns?.Resolve().ToString("X2") ?? "";
        public int TargetEnemyHandle => PlayerIns?.ReadInt32((int)EROffsets.EnemyIns.EnemyHandle) ?? 0;
        public int TargetEnemyArea => PlayerIns?.ReadInt32((int)EROffsets.EnemyIns.EnemyArea) ?? 0;
        private PHPointer? TargetEnemyModuleBase;
        private PHPointer? TargetEnemyData;
        private PHPointer? TargetEnemyResistance;
        private PHPointer? TargetEnemyStagger;

        private int TargetHandle => _targetEnemyIns?.ReadInt32((int)EROffsets.EnemyIns.EnemyHandle) ?? 0;
        public string TargetModel => TargetEnemyData?.ReadString((int)EROffsets.EnemyData.Model, Encoding.Unicode, 0x10) ?? "No Target";
        public string TargetName => TargetEnemyData?.ReadString((int)EROffsets.EnemyData.Name, Encoding.Unicode, 0x28) ?? "No Target";
        public int TargetChrType
        {
            get => _targetEnemyIns?.ReadInt32((int)EROffsets.EnemyIns.ChrType) ?? 0;
            set => _targetEnemyIns?.WriteInt32((int)EROffsets.EnemyIns.ChrType, value);
        }
        public int TargetHp
        {
            get => TargetEnemyData?.ReadInt32((int)EROffsets.EnemyData.Hp) ?? 0;
            set => _ = value; 
        }
        public int TargetHpMax
        {
            get => TargetEnemyData?.ReadInt32((int)EROffsets.EnemyData.HpMax) ?? 0;
            set => _ = value;

        }
        public int TargetFp
        {
            get => TargetEnemyData?.ReadInt32((int)EROffsets.EnemyData.Fp) ?? 0;
            set => _ = value;

        }
        public int TargetFpMax
        {
            get => TargetEnemyData?.ReadInt32((int)EROffsets.EnemyData.FpMax) ?? 0;
            set => _ = value;

        }
        public int TargetStam
        {
            get => TargetEnemyData?.ReadInt32((int)EROffsets.EnemyData.Stam) ?? 0;
            set => _ = value;

        }
        public int TargetStamMax
        {
            get => TargetEnemyData?.ReadInt32((int)EROffsets.EnemyData.StamMax) ?? 0;
            set => _ = value;

        }
        public int TargetPoison
        {
            get => TargetEnemyResistance?.ReadInt32((int)EROffsets.ResistenceData.Poison) ?? 0;
            set => _ = value;

        }
        public int TargetPoisonMax
        {
            get => TargetEnemyResistance?.ReadInt32((int)EROffsets.ResistenceData.PoisonMax) ?? 0;
            set => _ = value;

        }
        public int TargetRot
        {
            get => TargetEnemyResistance?.ReadInt32((int)EROffsets.ResistenceData.Rot) ?? 0;
            set => _ = value;

        }
        public int TargetRotMax
        {
            get => TargetEnemyResistance?.ReadInt32((int)EROffsets.ResistenceData.RotMax) ?? 0;
            set => _ = value;

        }
        public int TargetBleed
        {
            get => TargetEnemyResistance?.ReadInt32((int)EROffsets.ResistenceData.Bleed) ?? 0;
            set => _ = value;

        }
        public int TargetBleedMax
        {
            get => TargetEnemyResistance?.ReadInt32((int)EROffsets.ResistenceData.BleedMax) ?? 0;
            set => _ = value;

        }
        public int TargetFrost
        {
            get => TargetEnemyResistance?.ReadInt32((int)EROffsets.ResistenceData.Frost) ?? 0;
            set => _ = value;

        }
        public int TargetFrostMax
        {
            get => TargetEnemyResistance?.ReadInt32((int)EROffsets.ResistenceData.FrostMax) ?? 0;
            set => _ = value;

        }
        public int TargetBlight
        {
            get => TargetEnemyResistance?.ReadInt32((int)EROffsets.ResistenceData.Blight) ?? 0;
            set => _ = value;

        }
        public int TargetBlightMax
        {
            get => TargetEnemyResistance?.ReadInt32((int)EROffsets.ResistenceData.BlightMax) ?? 0;
            set => _ = value;

        }
        public int TargetSleep
        {
            get => TargetEnemyResistance?.ReadInt32((int)EROffsets.ResistenceData.Sleep) ?? 0;
            set => _ = value;

        }
        public int TargetSleepMax
        {
            get => TargetEnemyResistance?.ReadInt32((int)EROffsets.ResistenceData.SleepMax) ?? 0;
            set => _ = value;

        }
        public int TargetMadness
        {
            get => TargetEnemyResistance?.ReadInt32((int)EROffsets.ResistenceData.Madness) ?? 0;
            set => _ = value;

        }
        public int TargetMadnessMax
        {
            get => TargetEnemyResistance?.ReadInt32((int)EROffsets.ResistenceData.MadnessMax) ?? 0;
            set => _ = value;

        }
        public float TargetStagger
        {
            get => TargetEnemyStagger?.ReadSingle((int)EROffsets.StaggerData.Stagger) ?? 0;
            set => _ = value;

        }
        public float TargetStaggerMax
        {
            get => TargetEnemyStagger?.ReadSingle((int)EROffsets.StaggerData.StaggerMax) ?? 0;
            set => _ = value;

        }
        public float TargetResetTime
        {
            get => TargetEnemyStagger?.ReadSingle((int)EROffsets.StaggerData.ResetTime) ?? 0;
            set => _ = value;
        }

        public void UpdateLastEnemy()
        {
            //var lol = TargetEnemyIns?.Resolve();

            //if (lol != null)
            //{
            //    var targetPtr = CreateChildPointer(TargetEnemyIns, 0x58, 0x18, 0xC0, 0x18);
            //    var pointer = targetPtr.Resolve().ToInt64();
            //    var npcParamPtr = NpcParam.Pointer.Resolve().ToInt64();

            //    foreach (var p in NpcParam.OffsetDict.Keys)
            //    {
            //        if (NpcParam.OffsetDict[p] + npcParamPtr == pointer)
            //            Console.WriteLine();
            //    }
            //}

            if (CurrentTargetHandle == -1 || CurrentTargetHandle == TargetHandle)
                return;

            GetTarget();
        }

        public void GetTarget()
        {
            TargetEnemyIns = null;
            PHPointer worldBlockChr = CreateBasePointer(WorldChrMan.Resolve() + (int)EROffsets.WorldChrMan.WorldBlockChr);
            int targetHandle = CurrentTargetHandle; //Only read from memory once
            int targetArea = CurrentTargetArea;

            while (true)
            {
                int numChrs = worldBlockChr.ReadInt32((int)EROffsets.WorldBlockChr.NumChr);
                PHPointer chrSet = CreateChildPointer(worldBlockChr, (int)EROffsets.WorldBlockChr.ChrSet);

                for (int j = 0; j <= numChrs; j++)
                {
                    PHPointer enemyIns = CreateChildPointer(chrSet, (j * (int)EROffsets.ChrSet.EnemyIns));
                    int enemyHandle = enemyIns.ReadInt32((int)EROffsets.EnemyIns.EnemyHandle);
                    int enemyArea = enemyIns.ReadInt32((int)EROffsets.EnemyIns.EnemyArea);

                    if (targetHandle == enemyHandle && targetArea == enemyArea)
                        TargetEnemyIns = enemyIns;

                    if (TargetEnemyIns != null)
                        return;
                }

                long assertVal = worldBlockChr.ReadInt64(0x80);
                if (assertVal == -1)
                    worldBlockChr = CreateBasePointer(worldBlockChr.Resolve() + 0x160);
                else
                    break;
            }

            TryGetEnemy(targetHandle, targetArea, (int)EROffsets.WorldChrMan.ChrSet1);

            if (TargetEnemyIns != null)
                return;

            TryGetEnemy(targetHandle, targetArea, (int)EROffsets.WorldChrMan.ChrSet2);

        }

        public void GetTargetBackup()
        {
            TargetEnemyIns = null;
            int count = WorldChrMan.ReadInt32((int)EROffsets.WorldChrMan.NumWorldBlockChr);
            PHPointer worldBlockChr = CreateBasePointer(WorldChrMan.Resolve() + (int)EROffsets.WorldChrMan.WorldBlockChr);
            int targetHandle = CurrentTargetHandle; //Only read from memory once
            int targetArea = CurrentTargetArea;

            for (int i = 0; i <= count; i++)
            {
                int numChrs = worldBlockChr.ReadInt32((int)EROffsets.WorldBlockChr.NumChr + (i * 0x160));
                PHPointer chrSet = CreateChildPointer(worldBlockChr, (int)EROffsets.WorldBlockChr.ChrSet + (i * 0x160));

                for (int j = 0; j <= numChrs; j++)
                {
                    PHPointer enemyIns = CreateBasePointer(chrSet.Resolve() + (j * (int)EROffsets.ChrSet.EnemyIns));
                    int enemyHandle = enemyIns.ReadInt32((int)EROffsets.EnemyIns.EnemyHandle);
                    int enemyArea = enemyIns.ReadInt32((int)EROffsets.EnemyIns.EnemyArea);

                    if (targetHandle == enemyHandle && targetArea == enemyArea)
                        TargetEnemyIns = enemyIns;

                    if (TargetEnemyIns != null)
                        return;
                }

            }

            TryGetEnemy(targetHandle, targetArea, (int)EROffsets.WorldChrMan.ChrSet1);

            if (TargetEnemyIns != null)
                return;

            TryGetEnemy(targetHandle, targetArea, (int)EROffsets.WorldChrMan.ChrSet2);

        }

        public void TryGetEnemy(int targetHandle, int targetArea, int offset)
        {
            PHPointer chrSet1 = CreateChildPointer(WorldChrMan, offset);
            int numEntries1 = chrSet1.ReadInt32((int)EROffsets.ChrSet.NumEntries);

            for (int i = 0; i <= numEntries1; i++)
            {
                int enemyHandle = chrSet1.ReadInt32(0x78 + (i * 0x10));
                int enemyArea = chrSet1.ReadInt32(0x78 + 4 + (i * 0x10));
                if (targetHandle == enemyHandle && targetArea == enemyArea)
                    TargetEnemyIns = CreateChildPointer(chrSet1, 0x78 + 8 + (i * 0x10));

                if (TargetEnemyIns != null)
                    return;
            }
        }
        #endregion

        public int Level => PlayerGameData.ReadInt32((int)EROffsets.Player.Level);
        public string LevelString => PlayerGameData?.ReadInt32((int)EROffsets.Player.Level).ToString() ?? "";

        #region Cheats

        byte[]? OriginalCombatCloseMap;
        private bool _enableMapCombat { get; set; }

        public bool EnableMapCombat
        {
            get => _enableMapCombat;
            set
            {
                _enableMapCombat = value;
                if (value)
                    EnableMapInCombat();
                else
                    DisableMapInCombat();
            }
        }

        private void EnableMapInCombat()
        {
            OriginalCombatCloseMap = CombatCloseMap.ReadBytes(0x0, 0x5);
            byte[]? assembly = new byte[] { 0x48, 0x31, 0xC0, 0x90, 0x90 };

            DisableOpenMap.WriteByte(0x0, 0xEB); //Write Jump
            CombatCloseMap.WriteBytes(0x0, assembly);
        }

        private void DisableMapInCombat()
        {
            DisableOpenMap.WriteByte(0x0, 0x74); //Write Jump Equals
            CombatCloseMap.WriteBytes(0x0, OriginalCombatCloseMap); //Place original bytes back for combat close map
        }
        private short WeatherParamID => WorldAreaWeather?.ReadInt16((int)EROffsets.WorldAreaWeather.WeatherParamID) ?? 0;
        private short ForceWeatherParamID 
        {
            set => WorldAreaWeather?.WriteInt16((int)EROffsets.WorldAreaWeather.ForceWeatherParamID, value); 
        }
        public enum WeatherTypes
        {
            [Description("Slightly Cloudy")]
            SlightlyCloudy = 0,
            Sunny = 1,
            Overcast = 10,
            [Description("Storm Clouds")]
            StormClouds = 11,
            Rain = 20,
            [Description("Heavy Rain")]
            HeavyRain = 21,
            Downpour = 30,
            Fog = 31,
            [Description("Light Snow")]
            LightSnow = 40,
            Snow = 41,
            [Description("Freezing Fog")]
            FreezingFog = 50,
            [Description("Deep Freezing Fog")]
            DeepFreezingFog = 51,
            [Description("Deep Freezing Rainy Fog")]
            DeepFreezingRainyFog = 52,
            Windy = 60,
            Blizzard = 81,
            [Description("Rain and Snow")]
            RainSnow = 82,
            Moonlight = 83,
            [Description("Light Fog")]
            ClearLightFog = 99,
            Weather1001 = 1001,
            Weather1010 = 1010,
            Weather1011 = 1011,
            Weather1020 = 1020,
            Weather1021 = 1021,
            Weather1040 = 1040,
            Weather1050 = 1050,
            Weather1051 = 1051,
            Weather1052 = 1052,
            Weather2010 = 2010,
            Weather2011 = 2011,
            Weather2020 = 2020,
            Weather2021 = 2021,
            Weather2110 = 2110,
            Weather2111 = 2111,
            Weather3010 = 3010,
            Weather3011 = 3011,
            Weather3101 = 3101,
            Weather3110 = 3110,
            Weather3111 = 3111,
            Weather3120 = 3120,
            Weather4000 = 4000,
            Weather4010 = 4010,
            Weather4011 = 4011,
            Weather4040 = 4040,
            Weather4110 = 4110,
            Weather4111 = 4111,
            Weather4140 = 4140,
            Weather4201 = 4201,
            Weather4210 = 4210,
            Weather4211 = 4211,
            Weather4220 = 4220,
            Weather4221 = 4221,
            Weather4230 = 4230,
            Weather4231 = 4231,
            Weather4240 = 4240,
            Weather4241 = 4241,
            Weather4250 = 4250,
            Weather4251 = 4251,
            Weather4252 = 4252,
            Weather4260 = 4260,
        }
        private WeatherTypes _selectedWeather;
        public WeatherTypes SelectedWeather {
            get => _selectedWeather;
            set 
            { 
                _selectedWeather = value;
                if (_forceWeather)
                    ForceWeatherParamID = (short)_selectedWeather;
            }
        }
        private WeatherTypes _lastSelectedWeather;

        private bool _forceWeather { get; set; }
        public bool ForceWeather
        {
            get { return _forceWeather; }
            set 
            {
                ForceWeatherParamID = (short)_selectedWeather;
                _forceWeather = value;

                if (_forceWeather)
                    _lastSelectedWeather = (WeatherTypes)WeatherParamID;
                else
                    ForceWeatherParamID = (short)_lastSelectedWeather;

            }
        }

        public void ForceSetWeather()
        {
            ForceWeatherParamID = (short)_selectedWeather;
        }

        #endregion

        #region ChrAsm
        public byte ArmStyle
        {
            get => PlayerGameData.ReadByte((int)EROffsets.ChrIns.ArmStyle);
            set
            {
                if (!Loaded)
                    return;
                PlayerGameData.WriteByte((int)EROffsets.ChrIns.ArmStyle, value);
            }
        }
        public int CurrWepSlotOffsetLeft
        {
            get => PlayerGameData.ReadInt32((int)EROffsets.ChrIns.CurrWepSlotOffsetLeft);
            set
            {
                if (!Loaded)
                    return;
                PlayerGameData.WriteInt32((int)EROffsets.ChrIns.CurrWepSlotOffsetLeft, value);
            }
        }
        public int CurrWepSlotOffsetRight
        {
            get => PlayerGameData.ReadInt32((int)EROffsets.ChrIns.CurrWepSlotOffsetRight);
            set
            {
                if (!Loaded)
                    return;
                PlayerGameData.WriteInt32((int)EROffsets.ChrIns.CurrWepSlotOffsetRight, value);
            }
        }
        public int RHandWeapon1
        {
            get => PlayerGameData.ReadInt32((int)EROffsets.ChrIns.RHandWeapon1);
            set
            {
                if (!Loaded)
                    return;
                PlayerGameData.WriteInt32((int)EROffsets.ChrIns.RHandWeapon1, value);
            }
        }
        public int RHandWeapon2
        {
            get => PlayerGameData.ReadInt32((int)EROffsets.ChrIns.RHandWeapon2);
            set
            {
                if (!Loaded)
                    return;
                PlayerGameData.WriteInt32((int)EROffsets.ChrIns.RHandWeapon2, value);
            }
        }
        public int RHandWeapon3
        {
            get => PlayerGameData.ReadInt32((int)EROffsets.ChrIns.RHandWeapon3);
            set
            {
                if (!Loaded)
                    return;
                PlayerGameData.WriteInt32((int)EROffsets.ChrIns.RHandWeapon3, value);
            }
        }
        public int LHandWeapon1
        {
            get => PlayerGameData.ReadInt32((int)EROffsets.ChrIns.LHandWeapon1);
            set
            {
                if (!Loaded)
                    return;
                PlayerGameData.WriteInt32((int)EROffsets.ChrIns.LHandWeapon1, value);
            }
        }
        public int LHandWeapon2
        {
            get => PlayerGameData.ReadInt32((int)EROffsets.ChrIns.LHandWeapon2);
            set
            {
                if (!Loaded)
                    return;
                PlayerGameData.WriteInt32((int)EROffsets.ChrIns.LHandWeapon2, value);
            }
        }
        public int LHandWeapon3
        {
            get => PlayerGameData.ReadInt32((int)EROffsets.ChrIns.LHandWeapon3);
            set
            {
                if (!Loaded)
                    return;
                PlayerGameData.WriteInt32((int)EROffsets.ChrIns.LHandWeapon3, value);
            }
        }
        public int Arrow1
        {
            get => PlayerGameData.ReadInt32((int)EROffsets.ChrIns.Arrow1);
            set
            {
                if (!Loaded)
                    return;
                PlayerGameData.WriteInt32((int)EROffsets.ChrIns.Arrow1, value);
            }
        }
        public int Arrow2
        {
            get => PlayerGameData.ReadInt32((int)EROffsets.ChrIns.Arrow2);
            set
            {
                if (!Loaded)
                    return;
                PlayerGameData.WriteInt32((int)EROffsets.ChrIns.Arrow2, value);
            }
        }
        public int Bolt1
        {
            get => PlayerGameData.ReadInt32((int)EROffsets.ChrIns.Bolt1);
            set
            {
                if (!Loaded)
                    return;
                PlayerGameData.WriteInt32((int)EROffsets.ChrIns.Bolt1, value);
            }
        }
        public int Bolt2
        {
            get => PlayerGameData.ReadInt32((int)EROffsets.ChrIns.Bolt2);
            set
            {
                if (!Loaded)
                    return;
                PlayerGameData.WriteInt32((int)EROffsets.ChrIns.Bolt2, value);
            }
        }
        #endregion

        private int OGRHandWeapon1 { get; set; }
        private PHPointer OGRHandWeapon1Param
        {
            get => CreateBasePointer(EquipParamWeapon.Pointer.Resolve() + EquipParamWeapon.OffsetDict[OGRHandWeapon1]);
        }
        private int OGRHandWeapon1SwordArtID
        {
            get => OGRHandWeapon1Param?.ReadInt32((int)EROffsets.EquipParamWeapon.SwordArtsParamId) ?? 0;
            set => OGRHandWeapon1Param?.WriteInt32((int)EROffsets.EquipParamWeapon.SwordArtsParamId, value);
        }
        private int OGLHandWeapon1 { get; set; }
        private PHPointer OGLHandWeapon1Param
        {
            get => CreateBasePointer(EquipParamWeapon.Pointer.Resolve() + EquipParamWeapon.OffsetDict[OGLHandWeapon1]);
        }
        private int OGLHandWeapon1SwordArtID
        {
            get => OGLHandWeapon1Param?.ReadInt32((int)EROffsets.EquipParamWeapon.SwordArtsParamId) ?? 0;
            set => OGLHandWeapon1Param?.WriteInt32((int)EROffsets.EquipParamWeapon.SwordArtsParamId, value);
        }

    }
}
