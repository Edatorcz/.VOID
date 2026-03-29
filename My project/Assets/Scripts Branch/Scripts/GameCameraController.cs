using System.Collections;
using UnityEngine;

/// <summary>
/// Ovládá plynulé přesuny kamery mezi pozicemi.
///
/// NASTAVENÍ:
/// 1. Přidej na Main Camera (nebo prázdný GO "CameraRig" s kamerou jako child).
/// 2. Vytvoř 2 prázdné GO:
///    - "CamPos_Default" – výchozí pozice kamery (kde vidí balíček + desku)
///    - "CamPos_Board"   – pohled nad hrací desku (shora/blíž na sloty)
///    → Nastav jim pozici + rotaci jak chceš, aby kamera koukala.
/// 3. Přetáhni je do Inspectoru na tento skript.
/// </summary>
public class GameCameraController : MonoBehaviour
{
    [Tooltip("Výchozí pozice kamery (normální pohled)")]
    public Transform defaultPosition;

    [Tooltip("Pozice kamery nad hrací deskou (pro výběr slotu)")]
    public Transform boardPosition;

    [Tooltip("Jak rychle se kamera přesouvá (v sekundách)")]
    public float transitionDuration = 0.6f;

    private Coroutine activeTransition;
    private bool isAtBoard;

    void Start()
    {
        // Nastav kameru na výchozí pozici
        if (defaultPosition != null)
        {
            transform.position = defaultPosition.position;
            transform.rotation = defaultPosition.rotation;
        }
    }

    void Update()
    {
        // W = toggle mezi default a board pohledem
        if (Input.GetKeyDown(KeyCode.W))
        {
            if (isAtBoard)
                MoveToDefaultView();
            else
                MoveToBoardView();
        }
    }

    /// <summary>Plynule přesuň kameru nad hrací desku.</summary>
    public void MoveToBoardView()
    {
        if (boardPosition == null) return;
        MoveTo(boardPosition);
        isAtBoard = true;
    }

    /// <summary>Plynule vrať kameru na výchozí pozici.</summary>
    public void MoveToDefaultView()
    {
        if (defaultPosition == null) return;
        MoveTo(defaultPosition);
        isAtBoard = false;
    }

    public bool IsAtBoard => isAtBoard;

    void MoveTo(Transform target)
    {
        if (activeTransition != null)
            StopCoroutine(activeTransition);
        activeTransition = StartCoroutine(TransitionCoroutine(target));
    }

    IEnumerator TransitionCoroutine(Transform target)
    {
        Vector3 startPos = transform.position;
        Quaternion startRot = transform.rotation;
        float elapsed = 0f;

        while (elapsed < transitionDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / transitionDuration);

            transform.position = Vector3.Lerp(startPos, target.position, t);
            transform.rotation = Quaternion.Slerp(startRot, target.rotation, t);

            yield return null;
        }

        transform.position = target.position;
        transform.rotation = target.rotation;
        activeTransition = null;
    }
}
