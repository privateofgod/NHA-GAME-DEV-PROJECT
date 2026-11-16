using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Playables;
using Cinemachine;

namespace TownLife
{
    public class TownIntroduction : MonoBehaviour
    {
        public PlayableDirector director;
        public Transform guide;
        public Transform player;
        public Camera gameplayCamera;
        public Behaviour input;
        public Behaviour chase;
        public float triggerDistance = 12;
        public GameObject caption;
        public GameObject[] virtualCameras;
        bool introduced, playing;
        bool inputWasEnabled, chaseWasEnabled, agentWasStopped;
        Vector3 cameraPosition;
        Quaternion cameraRotation;
        float fieldOfView;
        NavMeshAgent agent;
        CinemachineBrain brain;
        bool brainWasEnabled;
        int restoreFrames;
        Canvas gameplayHud;
        bool hudWasEnabled;
        void OnEnable()
        {
            if (director != null) director.stopped += Finished;
        }
        void Start() { SetCameras(false); }
        void SetCameras(bool active)
        {
            if (virtualCameras == null) return;
            foreach (var cameraObject in virtualCameras) if (cameraObject != null) cameraObject.SetActive(active);
        }
        void Update()
        {
            if (!introduced && player != null && guide != null && Vector3.Distance(player.position, guide.position) < triggerDistance) Begin();
            if (playing && (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Escape))) Skip();
        }
        public void Begin()
        {
            if (playing || director == null || gameplayCamera == null) return;
            introduced = playing = true;
            cameraPosition = gameplayCamera.transform.position;
            cameraRotation = gameplayCamera.transform.rotation;
            fieldOfView = gameplayCamera.fieldOfView;
            var hudTransform = gameplayCamera.transform.Find("Canvas");
            gameplayHud = hudTransform != null ? hudTransform.GetComponent<Canvas>() : null;
            if (gameplayHud != null) { hudWasEnabled = gameplayHud.enabled; gameplayHud.enabled = false; }
            brain = gameplayCamera.GetComponent<CinemachineBrain>();
            if (brain != null) { brainWasEnabled = brain.enabled; brain.enabled = true; }
            if (input != null) { inputWasEnabled = input.enabled; input.enabled = false; }
            if (chase != null) { chaseWasEnabled = chase.enabled; chase.enabled = false; }
            agent = player.GetComponent<NavMeshAgent>();
            if (agent != null && agent.enabled && agent.isOnNavMesh)
            { agentWasStopped = agent.isStopped; agent.isStopped = true; agent.velocity = Vector3.zero; }
            var animator = player.GetComponent<Animator>();
            if (animator != null) animator.SetFloat("Speed_f", 0);
            director.time = 0;
            SetCameras(true);
            director.Play();
        }
        void Finished(PlayableDirector unused) { Restore(); }
        public void Skip()
        {
            if (!playing) return;
            director.Stop();
            Restore();
        }
        void Restore()
        {
            if (!playing) return;
            playing = false;
            SetCameras(false);
            if (brain != null) brain.enabled = false;
            restoreFrames = 2;
            if (input != null) input.enabled = inputWasEnabled;
            if (gameplayHud != null) gameplayHud.enabled = hudWasEnabled;
            if (chase != null) chase.enabled = chaseWasEnabled;
            if (agent != null && agent.enabled && agent.isOnNavMesh) agent.isStopped = agentWasStopped;
            if (gameplayCamera != null)
            {
                gameplayCamera.transform.SetPositionAndRotation(cameraPosition, cameraRotation);
                gameplayCamera.fieldOfView = fieldOfView;
            }
            if (caption != null) caption.SetActive(false);
        }
        void LateUpdate()
        {
            if (playing || restoreFrames <= 0) return;
            gameplayCamera.fieldOfView = fieldOfView;
            if (--restoreFrames == 0 && brain != null) brain.enabled = brainWasEnabled;
        }
        void OnGUI()
        {
            if (!playing) return;
            var old = GUI.color;
            GUI.color = Color.black;
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height * .08f), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(0, Screen.height * .84f, Screen.width, Screen.height * .16f), Texture2D.whiteTexture);
            GUI.color = old;
            if (caption != null && caption.activeSelf)
            {
                var style = new GUIStyle(GUI.skin.label);
                style.alignment = TextAnchor.MiddleCenter;
                style.fontSize = Mathf.Clamp(Screen.width / 38, 14, 30);
                style.normal.textColor = new Color(1, .85f, .55f);
                GUI.Label(new Rect(12, Screen.height * .845f, Screen.width - 24, Screen.height * .15f),
                    "RAKA  /  PEMANDU KOTA\nSelamat datang di taman dan Pasar Senja!", style);
            }
            if (GUI.Button(new Rect(Screen.width - 155, 12, 140, 36), "Lewati  [Spasi]")) Skip();
        }
        void OnDisable()
        {
            Skip();
            if (director != null) director.stopped -= Finished;
            if (brain != null && restoreFrames > 0) brain.enabled = brainWasEnabled;
        }
    }
}
