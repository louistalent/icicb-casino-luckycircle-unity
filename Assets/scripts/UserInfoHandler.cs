using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UserInfoHandler : MonoBehaviour
{
    public TMP_Text txt_username;
    public TMP_Text betAmount;
    public TMP_Text payOutText;
    public TMP_Text winAmountText;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void SetValues(string username, float amount, float payOut)
    {
        txt_username.text = username;
        betAmount.text = amount.ToString("F1");
        payOutText.text = payOut.ToString("F1") + "x";
        winAmountText.text = (amount * payOut).ToString("F1");
    }

}
