// using UnityEngine;

// [RequireComponent(typeof(Rigidbody))]
// public class PlayerFallHandler : MonoBehaviour
// {
//     private Rigidbody rb;
//     private bool fallRequested = false;
    
//     private bool isFalling = false;

//     private SmoothCubeRollController controller;

//     void Awake()
//     {
//         rb = GetComponent<Rigidbody>();
//         controller = GetComponent<SmoothCubeRollController>();
//     }

//     public void TriggerFall()
//     {
//         if (isFalling) return;

//         isFalling = true;

//         fallRequested = true;

//         // 🔴 Disable control while falling
//         controller.enabled = false;

//         rb.isKinematic = false;
//         rb.useGravity = true;

//         rb.AddForce(Vector3.down * 2f, ForceMode.Impulse);

//         // Simulate "landing recovery"
//         Invoke(nameof(RecoverFromFall), 0.5f); // tweak timing if needed
//     }

//     public bool HasPendingFall()
//     {
//         return fallRequested;
//     }

//     public void ExecuteFall()
//     {
//         StopAllCoroutines(); // 🔥 kills any stuck roll instantly
//         if (isFalling) return;


//         fallRequested = false;
//         isFalling = true;

//         controller.enabled = false;

//         rb.isKinematic = false;
//         rb.useGravity = true;

//         rb.linearVelocity = Vector3.zero;
//         rb.angularVelocity = Vector3.zero;

//         rb.AddForce(Vector3.down * 2f, ForceMode.Impulse);

//         Invoke(nameof(RecoverFromFall), 0.5f);
//     }

//     void RecoverFromFall()
//     {
//         // 🧹 STOP ALL PHYSICS COMPLETELY
//         rb.linearVelocity = Vector3.zero;
//         rb.angularVelocity = Vector3.zero;

//         rb.useGravity = false;
//         rb.isKinematic = true;

//         // 🧠 SNAP BACK TO GRID (MOST IMPORTANT PART)
//         SnapToGrid();

//         Physics.SyncTransforms();

//         // 🟢 Re-enable control
//         controller.enabled = true;

//         isFalling = false;
//     }

//     void SnapToGrid()
//     {
//         // Position snap
//         Vector3 p = transform.position;
//         transform.position = new Vector3(
//             Mathf.Round(p.x),
//             Mathf.Round(p.y),
//             Mathf.Round(p.z)
//         );

//         // Rotation snap (critical)
//         Vector3 r = transform.eulerAngles;
//         transform.rotation = Quaternion.Euler(
//             Mathf.Round(r.x / 90f) * 90f,
//             Mathf.Round(r.y / 90f) * 90f,
//             Mathf.Round(r.z / 90f) * 90f
//         );
//     }
// }