using UnityEngine;
using Unity.WebRTC;
using UnityEngine.UI;
using Cysharp.Threading.Tasks;

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
            HandleSignalingAsync(localConnection, remoteConnection).Forget();
        });
    }

    private void ReceiveChannelCallback(RTCDataChannel channel)
    { }

    private async UniTask HandleSignalingAsync(RTCPeerConnection localConnection, RTCPeerConnection remoteConnection)
    {
        // オファーSDPの作成
        var offerOptions = localConnection.CreateOffer();
        await UniTask.WaitUntil(() => offerOptions.IsDone);
        if (offerOptions.IsError)
        {
            Debug.LogError(offerOptions.Error.message);
            return;
        }
        var offerDesc = offerOptions.Desc;

        // ローカルピアにOffer SDPを設定
        var setLocalOp = localConnection.SetLocalDescription(ref offerDesc);
        await UniTask.WaitUntil(() => setLocalOp.IsDone);
        if (setLocalOp.IsError)
        {
            Debug.LogError(setLocalOp.Error.message);
            return;
        }

        // リモートピアにOffer SDPを設定
        var setRemoteOp = remoteConnection.SetRemoteDescription(ref offerDesc);
        await UniTask.WaitUntil(() => setLocalOp.IsDone);
        if (setLocalOp.IsError)
        {
            Debug.LogError(setLocalOp.Error.message);
            return;
        }

        // Anserの作成
        var answerOp = remoteConnection.CreateAnswer();
        await UniTask.WaitUntil(() => answerOp.IsDone);
        if (answerOp.IsError)
        {
            Debug.LogError(answerOp.Error.message);
            return;
        }
        var answerDesc = answerOp.Desc;

        // リモートピアにAnswer SDPを設定
        var setLocalOp2 = remoteConnection.SetLocalDescription(ref answerDesc);
        await UniTask.WaitUntil(() => setLocalOp2.IsDone);
        if (setLocalOp2.IsError)
        {
            Debug.LogError(setLocalOp2.Error.message);
            return;
        }

        // ローカルピアにAnswer SDPを設定
        var setRemoteOp2 = localConnection.SetRemoteDescription(ref answerDesc);
        await UniTask.WaitUntil(() => setRemoteOp2.IsDone);
        if (setRemoteOp2.IsError)
        {
            Debug.LogError(setRemoteOp2.Error.message);
            return;
        }

        Debug.Log("Signaling done.");
    }
}