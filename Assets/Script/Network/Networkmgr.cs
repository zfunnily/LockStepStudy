using System;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

using Random = UnityEngine.Random;

namespace Game.Network {
    using Utility;
    using Field;
    using UI;

    public class Networkmgr : MonoBehaviour {
        public const float STDDT = 0.017f;
        public static event Action UpdateEvent;
        public static event Action LateUpdateEvent;
        public const int WAITTING_INTERVAL = 5;
        private static Networkmgr INSTANCE;
        private const int DT = 17;
        private const int LEAST_COMPARE_FRAME = 65;
        private const string VERSION_CONTENT = "Version is old, please update.";
        private const string FULL_CONTENT = "Quota is full, please retry.";

        public static bool Connect() {
            return INSTANCE.client.Connect();
        }

        public static bool Disconnect(byte exitCode=ExitCode.Normal) {
            return INSTANCE.client.Disconnect(exitCode);
        }

        public static bool WillElaste {
            get;
            set;
        }

        public static float MovingValue {
            get;
            set;
        }

        [SerializeField]
        private string serverAddress;
        [SerializeField]
        private int serverPort;
        [SerializeField]
        private int version;
        [SerializeField]
        private Slot startGameSlot;
        [SerializeField]
        private Slot exitMatchSlot;
        private int updateTimer;
        private int frame;
        private int playFrame;
        private List<PlayData> playDataList;
        private Client client;
        private bool online;
        private string addr;
        private byte exitCode;
        private bool sendInLoop;

        protected void Awake() {
            INSTANCE = this;

            this.playDataList = new List<PlayData>();

            this.client = new Client(this.serverAddress, this.serverPort);
            this.client.RegisterEvent(EventCode.Connect, this.OnConnect);
            this.client.RegisterEvent(EventCode.Disconnect, this.OnDisconnect);
            this.client.RegisterEvent(EventCode.Start, this.OnStart);
            this.client.RegisterEvent(EventCode.Input, this.OnReceivePlayData);
            
            DOTween.defaultUpdateType = UpdateType.Manual;
            DOTween.Init();
            Networkmgr.LateUpdateEvent += () => DOTween.ManualUpdate(STDDT, STDDT);
        }

        protected void Update() {
            this.updateTimer += Mathf.CeilToInt(Time.deltaTime * 1000);

            while (this.updateTimer >= DT) {
                this.client.Update();

                if (this.playDataList.Count > 1) {
                    var lateFrame = this.frame;
                    this.sendInLoop = true;

                    do {
                        this.client.Update();
                        this.LockUpdate(true);
                    } while(this.playDataList.Count == 1 && this.frame == lateFrame);
                }

                this.LockUpdate();
                this.updateTimer -= DT;
            }

            if (this.exitCode != ExitCode.None) {
                this.addr = null;
                this.online = false;

                if (Judge.GameType == GameType.PVP) {
                    Judge.GameType = GameType.PVE;
                }
                else {
                    if (this.exitCode == ExitCode.Normal) {
                        this.exitMatchSlot.Run(this.gameObject);
                    }
                    else if (this.exitCode == ExitCode.Version) {
                        Interface.LabelContent = VERSION_CONTENT;
                    }
                    else if (this.exitCode == ExitCode.Full) {
                        Interface.LabelContent = FULL_CONTENT;
                    }
                }

                print("Disconnected");
                this.exitCode = ExitCode.None;
            }
        }

        private void LockUpdate(bool inLoop=false) {
            if (this.online && this.frame + 1 == WAITTING_INTERVAL && this.playDataList.Count == 0) {
                return;
            }

            if (this.online) {
                this.frame++;

                if (this.frame == WAITTING_INTERVAL) {
                    var data = this.playDataList[0];
                    this.playDataList.RemoveAt(0);
                    
                    if (Judge.IsRunning && data.addrs != null) {
                        for (int i = 0; i < data.addrs.Length; i++) {
                            Judge.Input(data.addrs[i], data.inputs[i]);
                        }
                    }

                    this.playFrame++;
                    this.frame = 0;

                    if (!inLoop || (inLoop && this.sendInLoop)) {
                        var input = new Datas.Input() {
                            data = new InputData() {
                                movingValue = Networkmgr.MovingValue,
                                willElaste = Networkmgr.WillElaste
                            },
                            frame = this.playFrame
                        };

                        this.sendInLoop = false;
                        Networkmgr.WillElaste = false;
                        this.client.Send(EventCode.Input, input);
                    }
                    
                    if (this.playFrame > LEAST_COMPARE_FRAME) {
                        var comparison = new Datas.Comparison() {
                            playFrame = this.playFrame,
                            content = Judge.Comparison
                        };

                        this.client.Send(EventCode.Comparison, comparison);
                    }
                }
            }

            Networkmgr.UpdateEvent();
            Networkmgr.LateUpdateEvent();
        }

        private void OnConnect(byte id, string data) {
            Debug.Log("(OnConnect) data=" + data);
            var obj = JsonUtility.FromJson<Datas.Connect>(data);

            if (obj.version != this.version || obj.isFull) {
                byte exitCode = obj.isFull ? ExitCode.Full : ExitCode.Version;
                Networkmgr.Disconnect(exitCode);
            }
            else {
                this.client.Send(EventCode.Handshake, new Datas.Handshake() {
                    deviceModel = SystemInfo.deviceModel
                });
            }

            this.addr = obj.addr;
            print("Connected");
        }

        private void OnDisconnect(byte id, string data) {
            var obj = JsonUtility.FromJson<Datas.Disconnect>(data);
            this.exitCode = obj.exitCode;
        }

        private void OnStart(byte id, string data) {
            var obj = JsonUtility.FromJson<Datas.Start>(data);
            Random.InitState(obj.seed);
            Judge.PlayerType = this.addr == obj.leftAddr ? PlayerType.A : PlayerType.B;
            Judge.SetAddr(obj.leftAddr, obj.rightAddr);
            this.startGameSlot.Run(this.gameObject);
            this.online = true;
            this.updateTimer = 0;
            this.frame = 0;
            this.playFrame = 0;
            this.exitCode = ExitCode.None;
            this.sendInLoop = false;
            this.playDataList.Clear();
            this.playDataList.Add(new PlayData());
            Networkmgr.MovingValue = 0;
            Networkmgr.WillElaste = false;
        }

        private void OnReceivePlayData(byte id, string data) {
            Debug.Log("(OnReceivePlayData) data=" + data);
            this.playDataList.Add(JsonUtility.FromJson<PlayData>(data));
        }
    }
}
