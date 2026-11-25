using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class UpraviteljIgre : MonoBehaviour
{
    public static UpraviteljIgre instanca;

    public float vremenskoOgranicenje = 60f;
    public int minimalnoMrkvi = 5;

    public KonjKretanje konj;

    public TextMeshProUGUI tekstVremena;
    public TextMeshProUGUI tekstMrkvi;
    public TextMeshProUGUI tekstPoraza;
    public TextMeshProUGUI tekstPobjede;

    public Image medalja;
    public TextMeshProUGUI tekstMedalje;

    public GameObject panelPobjeda;
    public GameObject panelPoraz;
    public GameObject panelPauza;

    public AudioSource zvucnikEfekata;
    public AudioClip zvukMrkve;
    public AudioClip zvukPobjede;
    public AudioClip zvukPoraza;

    private int skupljenoMrkvi = 0;
    private float preostaloVrijeme;
    private bool igraGotova = false;
    private bool pauzirano = false;

    void Awake()
    {
        instanca = this;
    }

    void Start()
    {
        if (konj == null)
        {
            konj = FindFirstObjectByType<KonjKretanje>();
        }

        Time.timeScale = 1f;
        preostaloVrijeme = vremenskoOgranicenje;

        panelPobjeda.SetActive(false);
        panelPoraz.SetActive(false);
        panelPauza.SetActive(false);

        AzurirajUI();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape) && igraGotova == false)
        {
            if (pauzirano == true)
            {
                Nastavi();
            }
            else
            {
                Pauziraj();
            }
        }

        if (igraGotova == true || pauzirano == true)
        {
            return;
        }

        preostaloVrijeme = preostaloVrijeme - Time.deltaTime;

        if (preostaloVrijeme <= 0)
        {
            preostaloVrijeme = 0;
            Poraz("Isteklo je vrijeme!");
        }

        AzurirajUI();
    }

    public void SkupiMrkvu()
    {
        if (igraGotova == true)
        {
            return;
        }

        skupljenoMrkvi = skupljenoMrkvi + 1;
        PustiZvuk(zvukMrkve);
        AzurirajUI();
    }

    public void StigaoDoCilja()
    {
        if (igraGotova == true)
        {
            return;
        }

        if (skupljenoMrkvi < minimalnoMrkvi)
        {
            Poraz("Premalo mrkvi! Treba barem " + minimalnoMrkvi + ".");
            return;
        }

        int sljedeciLevel = SceneManager.GetActiveScene().buildIndex + 1;
        int otkljucano = PlayerPrefs.GetInt("OtkljucanaRazina", 1);
        if (sljedeciLevel > otkljucano)
        {
            PlayerPrefs.SetInt("OtkljucanaRazina", sljedeciLevel);
        }

        SpremiRezultat();

        igraGotova = true;
        ZaustaviIgru();
        PustiZvuk(zvukPobjede);
        panelPobjeda.SetActive(true);
    }

    void SpremiRezultat()
    {
        int razina = SceneManager.GetActiveScene().buildIndex;
        float vrijeme = vremenskoOgranicenje - preostaloVrijeme;

        int mjesto = UpisiNaPopis("Razina" + razina + "_", 3, vrijeme);
        PlayerPrefs.SetFloat("Turnir_" + razina, vrijeme);
        PlayerPrefs.SetInt("TurnirMedalja_" + razina, mjesto);

        PrikaziMedalju(mjesto);

        string tekst = "Vrijeme: " + FormatVremena(vrijeme);
        tekst = tekst + "\nNajbolje: " + FormatVremena(PlayerPrefs.GetFloat("Razina" + razina + "_0"));

        if (razina == SceneManager.sceneCountInBuildSettings - 1)
        {
            tekst = tekst + TekstTurnira(razina);
        }

        if (tekstPobjede != null)
        {
            tekstPobjede.text = tekst;
        }

        PlayerPrefs.Save();
    }

    int UpisiNaPopis(string kljuc, int broj, float vrijeme)
    {
        float[] popis = new float[broj];

        for (int i = 0; i < broj; i++)
        {
            popis[i] = PlayerPrefs.GetFloat(kljuc + i, 0f);
        }

        int mjesto = -1;

        for (int i = 0; i < broj; i++)
        {
            if (popis[i] <= 0f || vrijeme < popis[i])
            {
                mjesto = i;
                break;
            }
        }

        if (mjesto == -1)
        {
            return -1;
        }

        for (int i = broj - 1; i > mjesto; i--)
        {
            popis[i] = popis[i - 1];
        }

        popis[mjesto] = vrijeme;

        for (int i = 0; i < broj; i++)
        {
            PlayerPrefs.SetFloat(kljuc + i, popis[i]);
        }

        return mjesto;
    }

    void PrikaziMedalju(int mjesto)
    {
        if (medalja == null)
        {
            return;
        }

        if (mjesto == -1)
        {
            medalja.gameObject.SetActive(false);
            return;
        }

        medalja.gameObject.SetActive(true);

        Color[] boje =
        {
            new Color(0.95f, 0.76f, 0.19f),
            new Color(0.8f, 0.8f, 0.82f),
            new Color(0.8f, 0.5f, 0.2f)
        };

        medalja.color = boje[mjesto];

        if (tekstMedalje != null)
        {
            tekstMedalje.text = (mjesto + 1).ToString();
        }
    }

    string TekstTurnira(int razina)
    {
        float ukupno = 0f;
        int zlato = 0;
        int srebro = 0;
        int bronca = 0;

        for (int i = 1; i <= razina; i++)
        {
            ukupno = ukupno + PlayerPrefs.GetFloat("Turnir_" + i, 0f);
            int m = PlayerPrefs.GetInt("TurnirMedalja_" + i, -1);

            if (m == 0)
            {
                zlato = zlato + 1;
            }
            else if (m == 1)
            {
                srebro = srebro + 1;
            }
            else if (m == 2)
            {
                bronca = bronca + 1;
            }
        }

        int mjesto = UpisiNaPopis("Ljestvica_", 5, ukupno);

        string t = "\n\nPOBJEDA NA TURNIRU!";
        t = t + "\nUkupno vrijeme: " + FormatVremena(ukupno);
        t = t + "\nZlato: " + zlato + "   Srebro: " + srebro + "   Bronca: " + bronca;
        t = t + "\n\nLJESTVICA";

        for (int i = 0; i < 5; i++)
        {
            float v = PlayerPrefs.GetFloat("Ljestvica_" + i, 0f);

            if (v <= 0f)
            {
                continue;
            }

            t = t + "\n" + (i + 1) + ".  " + FormatVremena(v);

            if (i == mjesto)
            {
                t = t + "  <- novo";
            }
        }

        return t;
    }

    public static string FormatVremena(float sekunde)
    {
        int min = (int)(sekunde / 60);
        int sek = (int)(sekunde % 60);

        if (sek < 10)
        {
            return min + ":0" + sek;
        }

        return min + ":" + sek;
    }

    public void Poraz(string razlog)
    {
        if (igraGotova == true)
        {
            return;
        }

        igraGotova = true;
        tekstPoraza.text = razlog;
        ZaustaviIgru();
        PustiZvuk(zvukPoraza);
        panelPoraz.SetActive(true);
    }

    public void Pauziraj()
    {
        pauzirano = true;
        konj.enabled = false;
        panelPauza.SetActive(true);
        Time.timeScale = 0f;
    }

    public void Nastavi()
    {
        pauzirano = false;
        konj.enabled = true;
        panelPauza.SetActive(false);
        Time.timeScale = 1f;
    }

    void ZaustaviIgru()
    {
        konj.enabled = false;
        Time.timeScale = 0f;
    }

    void PustiZvuk(AudioClip zvuk)
    {
        if (zvucnikEfekata != null && zvuk != null)
        {
            zvucnikEfekata.PlayOneShot(zvuk);
        }
    }

    void AzurirajUI()
    {
        tekstMrkvi.text = "Mrkve: " + skupljenoMrkvi + " / " + minimalnoMrkvi;
        tekstVremena.text = "Vrijeme: " + FormatVremena(preostaloVrijeme);

        if (preostaloVrijeme <= 10)
        {
            tekstVremena.color = Color.red;
        }
        else
        {
            tekstVremena.color = Color.white;
        }
    }

    public void Ponovi()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void SljedecaRazina()
    {
        Time.timeScale = 1f;
        int sljedeciLevel = SceneManager.GetActiveScene().buildIndex + 1;

        if (sljedeciLevel < SceneManager.sceneCountInBuildSettings)
        {
            SceneManager.LoadScene(sljedeciLevel);
        }
        else
        {
            SceneManager.LoadScene(0);
        }
    }

    public void GlavniIzbornik()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(0);
    }
}