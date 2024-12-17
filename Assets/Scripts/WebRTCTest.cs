using UnityEngine;
using Unity.WebRTC;
using UnityEngine.UI;

public class WebRTCTest : MonoBehaviour
{
    [SerializeField] Button button;

    void Start()
    {
        button.onClick.AddListener(() =>
        {
            // ローカルピア作成
            var localConnection = new RTCPeerConnection();
            var sendChannel = localConnection.CreateDataChannel("SendChannel");

            // リモートピア作成
            var remoteConnection = new RTCPeerConnection();
            remoteConnection.OnDataChannel = ReceiveChannelCallback;

            // 通信経路候補の登録
            localConnection.OnIceCandidate = e =>
            {
                if (!string.IsNullOrEmpty(e.Candidate))
                {
                    remoteConnection.AddIceCandidate(e);
                }
            };
            remoteConnection.OnIceCandidate = e =>
            {
                if (!string.IsNullOrEmpty(e.Candidate))
                {
                    localConnection.AddIceCandidate(e);
                }
            };

            // シグナリング処理

        });
    }

    private void ReceiveChannelCallback(RTCDataChannel channel)
    { }
}