using UnityEngine;

public class OgradaGenerator : MonoBehaviour
{
    public GameObject segmentOgrade;
    public Material materijal;
    public float dodatnaRotacijaY = 90f;
    public bool dodajCollider = true;
    public bool iDesnaStrana = true;

    public Vector3[] tocke;
    public Vector3[] tockeDesno;

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
            Debug.LogWarning("Komad ograde je prekratak: " + mjere.size);
            return;
        }

        PostaviStranu(tocke, mjere);

        if (iDesnaStrana == true)
        {
            if (tockeDesno != null && tockeDesno.Length > 1)
            {
                PostaviStranu(tockeDesno, mjere);
            }
            else
            {
                Vector3[] zrcalno = new Vector3[tocke.Length];

                for (int i = 0; i < tocke.Length; i++)
                {
                    zrcalno[i] = new Vector3(-tocke[i].x, tocke[i].y, tocke[i].z);
                }

                PostaviStranu(zrcalno, mjere);
            }
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

    void PostaviStranu(Vector3[] put, Bounds mjere)
    {
        for (int t = 0; t < put.Length - 1; t++)
        {
            Vector3 a = put[t];
            Vector3 b = put[t + 1];

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
                NapraviZid(a, b, okret, duljina);
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

    void NapraviZid(Vector3 a, Vector3 b, Quaternion okret, float duljina)
    {
        GameObject zid = new GameObject("NevidljiviZid");
        zid.transform.SetParent(transform);
        zid.transform.position = (a + b) * 0.5f + okret * Vector3.up * 5f;
        zid.transform.rotation = okret;

        BoxCollider bc = zid.AddComponent<BoxCollider>();
        bc.size = new Vector3(1f, 10f, duljina + 1f);
    }
}