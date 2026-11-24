using UnityEngine;

public class ProtivnikKonj : MonoBehaviour
{
    public float traka = -4f;
    public float brzina = 10.5f;
    public float skokVisina = 1.8f;
    public float skokTrajanje = 0.6f;
    public Color boja = Color.white;
    public AudioSource zvucnik;
    public float pomakZaobilaska = 3f;
    public float rubStaze = 6f;
    public Vector3[] tocke;

    private Animator anim;
    private int trenutnaTocka = 1;
    private bool krenuo = false;
    private bool gotov = false;
    private float skokTimer = 0f;
    private Vector3 pozicijaNaStazi;
    private float pomak = 0f;
    private float ciljaniPomak = 0f;
    private float vrijemeZaobilaska = -10f;
    private ProtivnikKonj[] svi;
    private bool blokiran = false;

    void Start()
    {
        anim = GetComponent<Animator>();
        svi = FindObjectsByType<ProtivnikKonj>(FindObjectsSortMode.None);

        foreach (Renderer r in GetComponentsInChildren<Renderer>())
        {
            r.material.color = boja;
        }

        traka = transform.position.x;
        pozicijaNaStazi = new Vector3(traka, tocke[0].y, transform.position.z);
        transform.position = pozicijaNaStazi;
        OkreniPrema(Tocka(1));
    }

    void Update()
    {
        if (zvucnik != null)
        {
            if (Time.timeScale == 0f)
            {
                zvucnik.Pause();
            }
            else if (krenuo == true && gotov == false && zvucnik.isPlaying == false)
            {
                zvucnik.Play();
            }
        }

        if (krenuo == false)
        {
            if (Input.GetAxis("Vertical") > 0.1f)
            {
                krenuo = true;
            }
            else
            {
                PostaviAnimaciju(0f);
                return;
            }
        }

        if (gotov == true)
        {
            PostaviAnimaciju(0f);
            return;
        }

        if (KonjIspred() == true)
        {
            PostaviAnimaciju(0f);
            return;
        }

        ZaobidjiStijene();

        if (blokiran == true)
        {
            PostaviAnimaciju(0f);
            return;
        }

        Vector3 cilj = Tocka(trenutnaTocka);
        pozicijaNaStazi = Vector3.MoveTowards(pozicijaNaStazi, cilj, brzina * Time.deltaTime);
        OkreniPrema(cilj);

        if (Vector3.Distance(pozicijaNaStazi, cilj) < 0.05f)
        {
            trenutnaTocka = trenutnaTocka + 1;

            if (trenutnaTocka >= tocke.Length)
            {
                UdjiUStalu();
                return;
            }
        }

        pomak = Mathf.MoveTowards(pomak, ciljaniPomak, 4f * Time.deltaTime);

        if (skokTimer <= 0f && PreprekaIspred() == true)
        {
            skokTimer = skokTrajanje;
        }

        float visina = 0f;

        if (skokTimer > 0f)
        {
            skokTimer = skokTimer - Time.deltaTime;
            float t = 1f - Mathf.Clamp01(skokTimer / skokTrajanje);
            visina = Mathf.Sin(t * Mathf.PI) * skokVisina;
        }

        transform.position = TrenutnaPozicija() + Vector3.up * visina;
        PostaviAnimaciju(1.5f);
    }

    void UdjiUStalu()
    {
        gotov = true;

        if (zvucnik != null)
        {
            zvucnik.Stop();
        }

        foreach (Collider c in GetComponentsInChildren<Collider>())
        {
            c.enabled = false;
        }

        PostaviAnimaciju(0f);
    }

    Vector3 TrenutnaPozicija()
    {
        return pozicijaNaStazi + transform.right * pomak;
    }

    Vector3 Tocka(int i)
    {
        Vector3 p = tocke[i];

        if (i >= tocke.Length - 2)
        {
            return p;
        }

        Vector3 smjer;

        if (i == 0)
        {
            smjer = tocke[1] - tocke[0];
        }
        else
        {
            smjer = (tocke[i] - tocke[i - 1]).normalized + (tocke[i + 1] - tocke[i]).normalized;
        }

        smjer.y = 0f;
        Vector3 desno = Vector3.Cross(Vector3.up, smjer).normalized;

        return p + desno * traka;
    }

    void OkreniPrema(Vector3 cilj)
    {
        Vector3 smjer = cilj - pozicijaNaStazi;
        smjer.y = 0f;

        if (smjer.sqrMagnitude > 0.001f)
        {
            transform.rotation = Quaternion.LookRotation(smjer);
        }
    }

    void ZaobidjiStijene()
    {
        blokiran = false;
        RaycastHit hit;
        Vector3 pocetak = TrenutnaPozicija() + Vector3.up * 1f;

        if (Physics.SphereCast(pocetak, 0.7f, transform.forward, out hit, 8f, ~0, QueryTriggerInteraction.Ignore))
        {
            if (JeLiStijena(hit.collider) == true)
            {
                if (ciljaniPomak == 0f)
                {
                    float prvi = 1f;

                    if (transform.InverseTransformPoint(hit.point).x > 0f)
                    {
                        prvi = -1f;
                    }

                    if (MozeNa(prvi) == true)
                    {
                        ciljaniPomak = prvi * pomakZaobilaska;
                    }
                    else if (MozeNa(-prvi) == true)
                    {
                        ciljaniPomak = -prvi * pomakZaobilaska;
                    }
                    else if (hit.distance < 2.5f)
                    {
                        blokiran = true;
                    }
                }

                vrijemeZaobilaska = Time.time;
                return;
            }
        }

        if (Time.time - vrijemeZaobilaska > 1.5f)
        {
            ciljaniPomak = 0f;
        }
    }

    bool MozeNa(float smjer)
    {
        float x = traka + smjer * pomakZaobilaska;

        if (Mathf.Abs(x) > rubStaze)
        {
            return false;
        }

        foreach (ProtivnikKonj p in svi)
        {
            if (p == this)
            {
                continue;
            }

            float drugaTraka = p.traka + p.ciljaniPomak;

            if (Mathf.Abs(drugaTraka - x) < 2f && Vector3.Distance(p.pozicijaNaStazi, pozicijaNaStazi) < 8f)
            {
                return false;
            }
        }

        return true;
    }

    bool JeLiStijena(Collider c)
    {
        if (c.GetComponentInParent<Prepreka>() != null)
        {
            return false;
        }

        string ime = c.gameObject.name.ToLower();
        return ime.Contains("rock") || ime.Contains("stijen") || ime.Contains("kamen") || ime.Contains("tree") || ime.Contains("stablo");
    }

    bool KonjIspred()
    {
        RaycastHit hit;
        Vector3 pocetak = TrenutnaPozicija() + Vector3.up * 0.8f;

        if (Physics.Raycast(pocetak, transform.forward, out hit, 2.5f, ~0, QueryTriggerInteraction.Ignore))
        {
            if (hit.collider.GetComponentInParent<KonjKretanje>() != null)
            {
                return true;
            }

            ProtivnikKonj drugi = hit.collider.GetComponentInParent<ProtivnikKonj>();

            if (drugi != null && drugi != this)
            {
                return true;
            }
        }

        return false;
    }

    bool PreprekaIspred()
    {
        RaycastHit hit;
        Vector3 pocetak = TrenutnaPozicija() + Vector3.up * 0.8f;
        float udaljenost = brzina * skokTrajanje * 0.5f;

        if (Physics.Raycast(pocetak, transform.forward, out hit, udaljenost, ~0, QueryTriggerInteraction.Ignore))
        {
            if (hit.collider.GetComponentInParent<Prepreka>() != null)
            {
                return true;
            }
        }

        return false;
    }

    void PostaviAnimaciju(float vrijednost)
    {
        if (anim == null)
        {
            return;
        }

        anim.SetFloat("Vert", vrijednost, 0.1f, Time.deltaTime);
        anim.SetFloat("State", vrijednost, 0.1f, Time.deltaTime);
    }
}