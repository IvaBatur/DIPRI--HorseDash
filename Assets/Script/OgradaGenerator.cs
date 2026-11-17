using UnityEngine;

public class OgradaGenerator : MonoBehaviour
{
    public GameObject segmentOgrade;
    public Material materijal;
    public float dodatnaRotacijaY = 90f;
    public bool dodajCollider = true;
    public bool iDesnaStrana = true;

    public Vector3[] tocke =
    {
        new Vector3(-7.2f, 0.51f, -100f),
        new Vector3(-7.2f, 0.51f, 92.45f),
        new Vector3(-7.2f, 11.47f, 133.5f),
        new Vector3(-7.2f, 11.47f, 233f)
    };

    [ContextMenu("Generiraj ogradu")]
    public void Generiraj()
    {
        if (segmentOgrade == null)
        {
            Debug.LogWarning("Povuci model ograde u polje Segment Ograde.");
            return;
        }

        Obrisi();

        Bounds mjere = IzmjeriKomad();

        if (mjere.size.z < 0.5f)
        {
            Debug.LogWarning("Komad ograde je prekratak. Promijeni Dodatna Rotacija Y na 0 ili 90.");
            return;
        }

        PostaviStranu(1f, mjere);

        if (iDesnaStrana == true)
        {
            PostaviStranu(-1f, mjere);
        }

#if UNITY_EDITOR
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(gameObject.scene);
#endif
    }

    [ContextMenu("Obrisi ogradu")]
    public void Obrisi()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            DestroyImmediate(transform.GetChild(i).gameObject);
        }
    }

    Bounds IzmjeriKomad()
    {
        GameObject probni = Instantiate(segmentOgrade, Vector3.zero, Quaternion.Euler(0f, dodatnaRotacijaY, 0f));
        Renderer[] dijelovi = probni.GetComponentsInChildren<Renderer>();

        Bounds mjere = new Bounds(Vector3.zero, Vector3.zero);

        if (dijelovi.Length > 0)
        {
            mjere = dijelovi[0].bounds;

            foreach (Renderer r in dijelovi)
            {
                mjere.Encapsulate(r.bounds);
            }
        }

        DestroyImmediate(probni);
        return mjere;
    }

    void PostaviStranu(float strana, Bounds mjere)
    {
        for (int t = 0; t < tocke.Length - 1; t++)
        {
            Vector3 a = tocke[t];
            Vector3 b = tocke[t + 1];
            a.x = a.x * strana;
            b.x = b.x * strana;

            Vector3 smjer = b - a;
            float duljina = smjer.magnitude;

            if (duljina < 0.01f)
            {
                continue;
            }

            smjer = smjer / duljina;
            Quaternion okret = Quaternion.LookRotation(smjer);

            int brojKomada = Mathf.CeilToInt(duljina / mjere.size.z);
            float korak = duljina / brojKomada;
            Vector3 pomak = new Vector3(mjere.center.x, mjere.min.y, mjere.center.z);

            for (int i = 0; i < brojKomada; i++)
            {
                Vector3 sredina = a + smjer * (i * korak + korak * 0.5f);

                GameObject komad = Instantiate(segmentOgrade, transform);
                komad.transform.position = sredina - okret * pomak;
                komad.transform.rotation = okret * Quaternion.Euler(0f, dodatnaRotacijaY, 0f);

                if (materijal != null)
                {
                    ObojiKomad(komad);
                }
            }

            if (dodajCollider == true)
            {
                NapraviZid(a, b, okret, duljina, strana);
            }
        }
    }

    void ObojiKomad(GameObject komad)
    {
        foreach (Renderer r in komad.GetComponentsInChildren<Renderer>())
        {
            Material[] novi = new Material[r.sharedMaterials.Length];

            for (int m = 0; m < novi.Length; m++)
            {
                novi[m] = materijal;
            }

            r.sharedMaterials = novi;
        }
    }

    void NapraviZid(Vector3 a, Vector3 b, Quaternion okret, float duljina, float strana)
    {
        GameObject zid = new GameObject("NevidljiviZid");
        zid.transform.SetParent(transform);

        Vector3 vani = Vector3.left * strana;
        zid.transform.position = (a + b) * 0.5f + vani * 0.5f + okret * Vector3.up * 5f;
        zid.transform.rotation = okret;

        BoxCollider bc = zid.AddComponent<BoxCollider>();
        bc.size = new Vector3(1f, 10f, duljina + 1f);
    }
}