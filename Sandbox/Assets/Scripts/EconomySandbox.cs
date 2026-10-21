using AmberScreen.Dev;
using AmberScreen.Economy;
using AmberScreen.Economy.TempSave;
using UnityEngine;

namespace AmberScreen.Sandbox
{
    /// <summary>
    /// Tiny Economy playtest: Add / Spend / Reserve / Release / Commit / Save / Load.
    /// Button amounts are major currency units (converted with <see cref="Money.ToMinor"/>).
    /// </summary>
    public sealed class EconomySandbox : MonoBehaviour
    {
        const string SaveKey = "economy_sandbox_wallet";

        Wallet m_Wallet;
        WalletSaveState m_Save;
        string m_HoldId;
        string m_Status = "Ready";

        void Awake()
        {
            m_Wallet = new Wallet();
            m_Save = new WalletSaveState(m_Wallet, SaveKey);
            m_Wallet.Changed += RefreshHud;
            RefreshHud();
        }

        void OnDestroy()
        {
            if (m_Wallet != null)
                m_Wallet.Changed -= RefreshHud;
        }

        void Start()
        {
            DevHud.SetSectionOrder("Economy");
            RefreshHud();
        }

        void RefreshHud()
        {
            if (m_Wallet == null) return;
            DevHud.SetLine("Economy", "Balance", () => Money.FormatMinor(m_Wallet.Balance));
            DevHud.SetLine("Economy", "Available", () => Money.FormatMinor(m_Wallet.Available));
        }

        void OnGUI()
        {
            if (m_Wallet == null) return;

            const float w = 220f;
            float x = 24f;
            float y = 24f;
            float row = 36f;

            GUI.Box(new Rect(x - 8f, y - 8f, w + 16f, row * 11f), GUIContent.none);
            GUI.Label(new Rect(x, y, w, row), "Economy sandbox");
            y += row;
            GUI.Label(new Rect(x, y, w, row), "Balance: " + Money.FormatMinor(m_Wallet.Balance));
            y += row;
            GUI.Label(new Rect(x, y, w, row), "Available: " + Money.FormatMinor(m_Wallet.Available));
            y += row;
            GUI.Label(new Rect(x, y, w, row), m_Status);
            y += row;

            if (GUI.Button(new Rect(x, y, w, row - 4f), "Add 100"))
            {
                m_Wallet.Add(Money.ToMinor(100f), "sandbox add");
                m_Status = "Added 100";
            }

            y += row;
            if (GUI.Button(new Rect(x, y, w, row - 4f), "Spend 30"))
            {
                m_Status = m_Wallet.TrySpend(Money.ToMinor(30f), "sandbox spend")
                    ? "Spent 30"
                    : "Spend failed (not enough Available)";
            }

            y += row;
            if (GUI.Button(new Rect(x, y, w, row - 4f), "Reserve 50"))
            {
                if (m_Wallet.TryReserve(Money.ToMinor(50f), "sandbox reserve", out m_HoldId))
                    m_Status = "Reserved 50";
                else
                    m_Status = "Reserve failed (not enough Available)";
            }

            y += row;
            if (GUI.Button(new Rect(x, y, w, row - 4f), "Release"))
            {
                m_Status = m_Wallet.Release(m_HoldId) ? "Released hold" : "Release failed (no hold)";
                m_HoldId = null;
            }

            y += row;
            if (GUI.Button(new Rect(x, y, w, row - 4f), "Commit"))
            {
                m_Status = m_Wallet.Commit(m_HoldId) ? "Committed hold" : "Commit failed (no hold)";
                m_HoldId = null;
            }

            y += row;
            if (GUI.Button(new Rect(x, y, w, row - 4f), "Save"))
            {
                JsonSaveBackend.Write(SaveKey, m_Save.Capture());
                m_Status = "Saved";
            }

            y += row;
            if (GUI.Button(new Rect(x, y, w, row - 4f), "Load"))
            {
                if (JsonSaveBackend.TryRead(SaveKey, out string json))
                {
                    m_Save.Restore(json);
                    m_HoldId = null;
                    m_Status = "Loaded";
                    RefreshHud();
                }
                else
                    m_Status = "Load failed (no file)";
            }
        }
    }
}
