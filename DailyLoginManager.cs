using UnityEngine;
using System;
using System.Globalization;

public class DailyLoginManager : MonoBehaviour
{
    private const string LastLoginKey = "LastLoginDate";
    private const string StreakKey    = "LoginStreak";
    private const string LastClaimKey = "LastClaimDate";

    private DateTime lastLoginDate;
    private int     loginStreak;

    [Header("Recompensas Diárias")]
    public RewardData[] rewards;  // Configure 7 elementos no Inspector
    public Wallet       wallet;   // Referência ao componente Wallet

    public event Action<int, bool> OnLoginChecked;

    void Start()
    {
        LoadData();
        CheckLogin();
    }

    private void LoadData()
    {
        string sd = PlayerPrefs.GetString(LastLoginKey, "");
        if (!string.IsNullOrEmpty(sd) &&
            DateTime.TryParseExact(sd, "yyyy-MM-dd", null,
                                   DateTimeStyles.None, out var dt))
        {
            lastLoginDate = dt;
        }
        else
        {
            lastLoginDate = DateTime.MinValue;
        }
        loginStreak = PlayerPrefs.GetInt(StreakKey, 0);
    }

    public void CheckLogin()
    {
        DateTime today = DateTime.Now.Date;
        Debug.Log($"[DailyLogin] CheckLogin em {today:yyyy-MM-dd}");

        string cd = PlayerPrefs.GetString(LastClaimKey, "");
        Debug.Log($"[DailyLogin] LastClaimDate = '{cd}'");

        if (!string.IsNullOrEmpty(cd) &&
            DateTime.TryParseExact(cd, "yyyy-MM-dd", null,
                                   DateTimeStyles.None, out var claimed) &&
            claimed == today)
        {
            Debug.Log("[DailyLogin] Já coletou hoje — não exibe painel.");
            return;
        }

        bool wasReset = false;
        if (lastLoginDate == DateTime.MinValue)
        {
            loginStreak = 1;
        }
        else
        {
            int diff = (today - lastLoginDate).Days;
            if (diff == 1)
                loginStreak = Mathf.Clamp(loginStreak + 1, 1, 7);
            else if (diff > 1)
            {
                loginStreak = 1;
                wasReset = true;
            }
        }

        if (loginStreak > 7)
        {
            GiveReward(7);
            loginStreak = 1;
            wasReset = true;
        }

        lastLoginDate = today;
        SaveData();

        Debug.Log($"[DailyLogin] Disparando evento OnLoginChecked para dia {loginStreak}");
        OnLoginChecked?.Invoke(loginStreak, wasReset);
    }

    private void SaveData()
    {
        PlayerPrefs.SetString(LastLoginKey, lastLoginDate.ToString("yyyy-MM-dd"));
        PlayerPrefs.SetInt(StreakKey, loginStreak);
        PlayerPrefs.Save();
    }

    public int GetCurrentStreak() => loginStreak;

    public void GiveReward(int day)
    {
        var data = Array.Find(rewards, r => r.day == day);
        if (data == null) return;

        if (data.coinAmount > 0)    wallet.AddCoins(data.coinAmount);
        if (data.capsuleAmount > 0) wallet.AddCapsules(data.capsuleAmount);

        // Marca como collect hoje
        string todayStr = DateTime.Now.Date.ToString("yyyy-MM-dd");
        PlayerPrefs.SetString(LastClaimKey, todayStr);
        PlayerPrefs.Save();

        Debug.Log($"[DailyLogin] Recompensa Dia {day}: +{data.coinAmount} coins, +{data.capsuleAmount} cápsulas.");
    }

    public void ResetStreak()
    {
        loginStreak = 0;
        lastLoginDate = DateTime.MinValue;
        PlayerPrefs.DeleteKey(LastClaimKey);
        SaveData();
    }
}
