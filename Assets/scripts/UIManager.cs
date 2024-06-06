using SimpleJSON;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnitySocketIO;
using UnitySocketIO.Events;
public class UIManager : MonoBehaviour
{
    public SocketIOController io;

    public TMP_Text walletAmount_Text;
    public TMP_Text info_Text;
    public GameObject infoTextObject;
    public Animator errorTextAnim;
    public TMP_Text leftTimeText;
    public TMP_Text leaveTileNumText;
    public TMP_InputField AmountField;
    BetPlayer _player = new BetPlayer();

    public Transform[] tiles = new Transform[195];

    private int[] pressTilePosition;
    private float[] pressTileValue;

    public GameObject prefab_userinfo;
    public TMP_Text BetBtnText;
    public Transform transform_content;
    public GameObject refreshImage;
    public Animator anim;

    private Transform selectedTile;

    public Users m_users;
    private List<SetResult> datas = new List<SetResult>();

    private bool isEnteredRoom = false;
    private bool gameEnd = false;
    private bool gameStarted = false;
    private bool canPressTile = true;
    private int flag = 0;

    const int PLAYING = 0;
    const int GAMEEND = 1;
    public enum GameState
    {
        PLAYING,
        GAMEEND
    }
    public static GameState myState = GameState.PLAYING;

    // GameReadyStatus Send
    [DllImport("__Internal")]
    private static extern void GameReady(string msg);

    // Start is called before the first frame update
    void Start()
    {
        AmountField.text = "10.0";
        datas.Clear();
        io.Connect();
        io.On("connect", (e) =>
        {
            Debug.Log("Game started");

            io.Emit("init tilestate", JsonUtility.ToJson(_player));

            io.On("init tileposition", (res) =>
            {
            InitTileState(res);
            });
        });

        StartCoroutine(iStart(0.5f));
        #if UNITY_WEBGL == true && UNITY_EDITOR == false
            GameReady("Ready");
        #endif
    }

    // Update is called once per frame
    void Update()
    {
        if (gameEnd)
        {
            canPressTile = false;
            refreshImage.SetActive(true);
            anim.SetBool("rotate", gameEnd);
            StartCoroutine(RefreshButton(3.5f));
        }
        else
        {
            refreshImage.SetActive(false);
            canPressTile = true;
        }
        StartCoroutine(UpdateState(0.5f));
    }

    private void InitTileState(SocketIOEvent socketIOEvent)
    {
        Response cres = JsonUtility.FromJson<Response>(socketIOEvent.data);
        Debug.Log("--------"+cres);
        pressTilePosition = cres.pressTilePosition;
        pressTileValue = cres.pressTileValue;

        for (int i = 0; i < pressTilePosition.Length; i++)
        {
            tiles[pressTilePosition[i]].GetComponent<Button>().interactable = false;
            if (tiles[pressTilePosition[i]].childCount != 0)
            {
                tiles[pressTilePosition[i]].GetChild(0).gameObject.GetComponent<TMP_Text>().text = cres.pressTileValue[i].ToString("F1");
            }
        }
    }

    IEnumerator UpdateState(float t)
    {
        io.Emit("state update", JsonUtility.ToJson(_player));
        yield return new WaitForSeconds(t);
    }

    IEnumerator RefreshButton(float t)
    {
        datas.Clear();
        yield return new WaitForSeconds(t);
        for(int i = 0; i < 195; i++)
        {
            tiles[i].GetComponent<Button>().interactable = true;
            tiles[i].GetChild(0).gameObject.GetComponent<TMP_Text>().text = "";
        }
    }

    IEnumerator ShowError(string errMessage)
    {
        infoTextObject.SetActive(true);
        info_Text.text = errMessage;
        errorTextAnim.SetBool("ShowError", true);
        yield return new WaitForSeconds(2.5f);
        info_Text.text = "";
        infoTextObject.SetActive(false);
    }

    IEnumerator iStart(float t)
    {
        yield return new WaitForSeconds(t);

        io.On("error message", (SocketIOEvent e) =>
        {
            Response res = JsonUtility.FromJson<Response>(e.data);
            Debug.Log(res.errMessage);
            StartCoroutine(ShowError(res.errMessage));
        });

        io.Emit("enterroom", JsonUtility.ToJson(_player), (string res) =>
        {

            Response cres = JsonUtility.FromJson<Response>(res);
            if (cres.status == 1)
            {
                isEnteredRoom = true;
                pressTilePosition = cres.pressTilePosition;
                pressTileValue = cres.pressTileValue;

                for (int i = 0; i < pressTilePosition.Length; i++)
                {
                    tiles[pressTilePosition[i]].GetComponent<Button>().interactable = false;
                    if (tiles[pressTilePosition[i]].childCount != 0)
                    {
                        tiles[pressTilePosition[i]].GetChild(0).gameObject.GetComponent<TMP_Text>().text = cres.pressTileValue[i].ToString("F1");
                    }
                }
            }
            else
            {
                isEnteredRoom = false;
            }
        });

        io.On("update state", (SocketIOEvent e) =>
        {

            if (!isEnteredRoom)
                return;

            Response res = JsonUtility.FromJson<Response>(e.data);
            leaveTileNumText.text = res.tileNum.ToString();
            switch (res.gamestate)
            {
                case PLAYING:
                    myState = GameState.PLAYING;
                    gameEnd = false;
                    leftTimeText.text = ((int)((res.leftTime / 10000f) * 10)).ToString();
                    break;
                case GAMEEND:
                    myState = GameState.GAMEEND;
                    gameEnd = true;
                    for(int i = 0; i < 195; i++)
                    {
                        tiles[i].GetComponent<Button>().interactable = false;
                        tiles[i].GetChild(0).gameObject.GetComponent<TMP_Text>().text = res.totalTileValue[i].ToString("F1");
                    }                   
                    break;
            }

        });

        io.On("press tile", (SocketIOEvent e) =>
        {
            Response res = JsonUtility.FromJson<Response>(e.data);
            tiles[res.tileIndex].GetComponent<Button>().interactable = false;
            if (tiles[res.tileIndex].childCount != 0)
            {
                for (int i = 0; i < tiles[res.tileIndex].childCount; i++)
                {
                    tiles[res.tileIndex].GetChild(i).gameObject.GetComponent<TMP_Text>().text = res.tileValue.ToString("F1");
                }
            }
        });

        io.On("userslist", (SocketIOEvent e) =>
        {
            m_users = Users.CreateFromJson(e.data);            

            JSONNode usersInfo = JSON.Parse(e.data);
            Debug.Log(usersInfo["users"]);

            SetResult resResult = new SetResult();
            resResult.username = usersInfo["users"]["name"];
            resResult.amount = usersInfo["users"]["betAmount"];
            resResult.payOut = usersInfo["users"]["payOut"];
            datas.Add(resResult);

            if (transform_content.childCount != 0)
            {
                for (int i = 0; i < transform_content.childCount; i++)
                {
                    Destroy(transform_content.GetChild(i).gameObject);
                }
            }

            for (int i = datas.Count -1; i >= 0; i--)
            {
                GameObject cell = Instantiate(prefab_userinfo, transform_content);                
                cell.GetComponent<UserInfoHandler>().SetValues(datas[i].username,datas[i].amount, datas[i].payOut);                
            }
        });
    }
    // Receive token and amount , username from React
    public void RequestToken(string data)
    {
        JSONNode usersInfo = JSON.Parse(data);
        Debug.Log("token=--------" + usersInfo["token"]);
        Debug.Log("amount=------------" + usersInfo["amount"]);
        Debug.Log("userName=------------" + usersInfo["userName"]);
        _player.token = usersInfo["token"];
        _player.username = usersInfo["userName"];

        float i_balance = float.Parse(usersInfo["amount"]);
        walletAmount_Text.text = i_balance.ToString("F2");
    }

    public void MinBtn_Clicked()
    {
        AmountField.text = "10.0";
    }

    public void CrossBtn_Clicked()
    {
        float amount = float.Parse(AmountField.text);
        if (amount >= 100000f)
            AmountField.text = "100000.0";
        else
            AmountField.text = (amount * 2.0f).ToString("F2");
    }

    public void HalfBtn_Clicked()
    {
        float amount = float.Parse(AmountField.text);
        if (amount <= 10f)
            AmountField.text = "10.0";
        else
            AmountField.text = (amount / 2.0f).ToString("F2");
    }

    public void MaxBtn_Clicked()
    {
        float myTotalAmount = float.Parse(string.IsNullOrEmpty(walletAmount_Text.text) ? "0" : walletAmount_Text.text);
        if (myTotalAmount >= 100000f)
            AmountField.text = "100000.0";
        else if (myTotalAmount >= 10f && myTotalAmount < 100000f)
            AmountField.text = myTotalAmount.ToString("F2");
    }

    public void AmountField_Changed()
    {
        if (float.Parse(AmountField.text) < 10f)
            AmountField.text = "10.0";
        else if (float.Parse(AmountField.text) > 100000f)
        {
            AmountField.text = "100000.0";
        }
    }

    public void BetBtnClicked()
    {
        flag++;
        if (flag % 2 == 1)
        {
            BetBtnText.text = "End";
            gameStarted = true;
        }
        else
        {
            BetBtnText.text = "Start";
            gameStarted = false;
        }
    }

    public void TileClickObject(Transform obj)
    {
        selectedTile = obj;
    }

    public void TileClickEvent(int index)
    {
        if (float.Parse(AmountField.text) <= float.Parse(walletAmount_Text.text))
        {
            if (gameStarted && canPressTile)
            {
                JsonType JObject = new JsonType();
                JObject.tileIndex = index;
                JObject.username = _player.username;
                JObject.amount = float.Parse(walletAmount_Text.text);
                JObject.betAmount = float.Parse(AmountField.text);
                JObject.token = _player.token;

                io.Emit("tile click", JsonUtility.ToJson(JObject), (string res) =>
                {
                    Response cres = JsonUtility.FromJson<Response>(res);
                    canPressTile = false;
                    leaveTileNumText.text = cres.tileNum.ToString();
                    walletAmount_Text.text = cres.amount.ToString("F2");
                    selectedTile.GetComponent<Button>().interactable = false;
                    if (selectedTile.childCount != 0)
                    {
                        for (int i = 0; i < selectedTile.childCount; i++)
                        {
                            selectedTile.GetChild(i).gameObject.GetComponent<TMP_Text>().text = cres.tileValue.ToString("F1");
                        }
                    }
                    StartCoroutine(SetPressTile(0.5f));
                });
            }
        }
        else
            StartCoroutine(ShowError("Not Enough Funds!"));
    }

    IEnumerator SetPressTile(float t)
    {
        yield return new WaitForSeconds(t);
        canPressTile = true;
    }
}

public class BetPlayer
{
    public string username;
    public string token;
}

public class JsonType
{
    public int tileIndex;
    public float betAmount;
    public float amount;
    public string username;
    public string token;
}

public class Users
{
    public List<BetPlayer> users;
    public static Users CreateFromJson(string data)
    {
        return JsonUtility.FromJson<Users>(data);
    }
}

public class Response
{
    public float amount;
    public int status;
    public int gamestate;
    public float leftTime;
    public int tileNum;
    public int tileIndex;
    public float tileValue;
    public float[] totalTileValue;
    public int[] pressTilePosition;
    public float[] pressTileValue;
    public string errMessage;
}

public class SetResult
{
    public string username;
    public float amount;
    public float payOut;
}
