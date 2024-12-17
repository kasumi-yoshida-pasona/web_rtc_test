using UnityEngine;
using Unity.WebRTC;
using UnityEngine.UI;
using Cysharp.Threading.Tasks;
using TMPro;

public class WebRTCTest : MonoBehaviour
{
    [SerializeField] private Button openBtn;
    [SerializeField] private Button closeBtn;
    [SerializeField] private TextMeshProUGUI text;
    [SerializeField] private TMP_InputField inputField;
    private RTCDataChannel sendChannel;
    private RTCDataChannel receiveChannel;
    private RTCPeerConnection localConnection;
    private RTCPeerConnection remoteConnection;

    void Start()
    {
        OpenBtnAvailable();

        openBtn.onClick.AddListener(() =>
        {
            // ローカルピア作成
            localConnection = new RTCPeerConnection();
            sendChannel = localConnection.CreateDataChannel("SendChannel");

            // リモートピア作成
            remoteConnection = new RTCPeerConnection();
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
            HandleSignalingAsync().Forget();
        });

        closeBtn.onClick.AddListener(() =>
        {
            // 終了処理
            sendChannel.Close();
            receiveChannel.Close();

            localConnection.Close();
            remoteConnection.Close();

            OpenBtnAvailable();
            Debug.Log("Close connection.");
        });
    }

    private void OpenBtnAvailable()
    {
        openBtn.interactable = true;
        closeBtn.interactable = false;
        text.text = "";
        inputField.interactable = false;
    }

    private void CloseBtnAvailable()
    {
        openBtn.interactable = false;
        closeBtn.interactable = true;
        text.text = "";
        inputField.interactable = true;
    }

    // ICEの交換終了時に呼び出されるコールバック
    private void ReceiveChannelCallback(RTCDataChannel channel)
    {
        receiveChannel = channel;
        receiveChannel.OnMessage = OnReceiveMessage;

        CloseBtnAvailable();
        Debug.Log($"Received DataChannel: {channel.Label}");

        inputField.onEndEdit.AddListener(text =>
        {
            if (sendChannel == null)
            {
                return;
            }

            var bytes = System.Text.Encoding.UTF8.GetBytes(text);
            sendChannel.Send(bytes);
            inputField.text = "";
        });
    }

    private void OnReceiveMessage(byte[] bytes)
    {
        var message = System.Text.Encoding.UTF8.GetString(bytes);
        text.text += message;
    }

    private async UniTask HandleSignalingAsync()
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

        // ICE コネクションの監視
        localConnection.OnIceConnectionChange = state =>
        {
            Debug.Log($"ICE connection state: {state}");
        };
    }
}