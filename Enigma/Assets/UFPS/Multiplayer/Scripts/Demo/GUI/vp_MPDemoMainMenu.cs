/////////////////////////////////////////////////////////////////////////////////
//
//	vp_MPDemoMainMenu.cs
//	© Opsive. All Rights Reserved.
//	https://twitter.com/Opsive
//	http://www.opsive.com
//
//	description:	a very simple main menu with buttons for logging in, setting
//					player name, toggling fullscreen / windowed mode and unity
//					quality	settings. provided for demo purposes only
//
//					NOTE: the level containing this component should be added to
//					the build settings as the topmost one (index 0).
//
/////////////////////////////////////////////////////////////////////////////////

using Photon_Unity_Networking.Plugins.PhotonNetwork;
using UFPS.Base.Scripts.Core.EventSystem;
using UFPS.Base.Scripts.Core.Utility;
using UFPS.Base.Scripts.Gameplay;
using UFPS.Multiplayer.Scripts.Master;
using UnityEngine;
using MonoBehaviour = Photon_Unity_Networking.Plugins.PhotonNetwork.MonoBehaviour;

namespace UFPS.Multiplayer.Scripts.Demo.GUI
{
    public class vp_MPDemoMainMenu : MonoBehaviour
    {

        // content
        public Texture2D Splash = null;
        public bool RequireSetName = false;
        public Font BigFont;
        public Font SmallFont;
        public Font MessageFont;
        public Texture BgTexture;

        // colors
        protected Color m_CurrentSplashColor = Color.white;
        protected Color m_TargetSplashColor = Color.white;
        protected Color m_StartButtonColor = Color.white;
        protected Color m_NameColor = Color.white;

        // buttons
        protected string m_DefaultStartButtonText = "";
        protected string m_StartButtonText = "Log On";
        protected string m_DefaultPlayerName = "";

        protected string m_FullScreenButtonText = "Fullscreen";
        protected string m_QualityButtonText = "";
        protected string[] m_Qualitys = new string[] { "Fastest", "Fast", "Simple", "Good", "Beautiful", "Fantastic" };
        protected int m_Quality = 0;

        // logic
        public new bool DontDestroyOnLoad = true;
        protected bool m_UserPressedConnect = false;
        protected bool m_UserHasHitReturn = false;
        protected Vector2 m_Pos = Vector2.zero;
        protected Vector2 m_Size = Vector2.zero;
        protected Rect m_LabelRect = new Rect(0, 0, 0, 0);

        // --- properties ---

        protected static vp_MPDemoChat m_Chat = null;
        public static vp_MPDemoChat Chat
        {
            get
            {
                if (m_Chat == null)
                    m_Chat = Component.FindObjectOfType<vp_MPDemoChat>();
                return m_Chat;
            }
        }


        /// <summary>
        /// 
        /// </summary>
        protected virtual void OnEnable()
        {

            vp_GlobalEvent.Register("DisableMultiplayerGUI", delegate() { vp_Utility.Activate(gameObject, false); });	// called from 'vp_MPSinglePlayerTest'
            vp_GlobalEvent.Register("Disconnected", Reset);			// called from 'vp_MPConnection' when player disconnects

        }


        /// <summary>
        /// 
        /// </summary>
        protected virtual void OnDisable()
        {

            vp_GlobalEvent.Unregister("DisableMultiplayerGUI", delegate() { vp_Utility.Activate(gameObject, false); });
            vp_GlobalEvent.Unregister("Disconnected", Reset);

        }

	
        /// <summary>
        /// 
        /// </summary>
        protected virtual void Start()
        {
            vp_Gameplay.PlayerName = "emp_recruit";
            m_DefaultPlayerName = vp_Gameplay.PlayerName;
            m_DefaultStartButtonText = m_StartButtonText;
            vp_MPConnection.StayConnected = false;
            m_Quality = QualitySettings.GetQualityLevel();
            Chat.enabled = false;

            if (DontDestroyOnLoad)
                Object.DontDestroyOnLoad(transform.root.gameObject);

        }


        /// <summary>
        /// 
        /// </summary>
        protected virtual void OnGUI()
        {
            UnityEngine.GUI.depth = 1;
            m_TargetSplashColor.a = ((PhotonNetwork.connectionStateDetailed == ClientState.Joined) ? 0.0f : 1.0f);

            m_CurrentSplashColor.a = Mathf.Lerp(m_CurrentSplashColor.a, m_TargetSplashColor.a, Time.deltaTime * 1.0f);
            if (Splash != null)
            {
                if (m_CurrentSplashColor.a > 0.01f)
                {
                    //Debug.Log("drawing");
                    UnityEngine.GUI.color = m_CurrentSplashColor;
                    UnityEngine.GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Splash);
                    UnityEngine.GUI.color = Color.white;
                }
            }

            if (PhotonNetwork.connectionStateDetailed == ClientState.Joined)
                return;

            UnityEngine.GUI.Label(new Rect(10, Screen.height - 25, 100, 20), vp_Gameplay.Version);

            UnityEngine.GUI.SetNextControlName("1");
            Vector2 startPos = new Vector2((Screen.width / 2) - (Screen.width / 3.5f), (Screen.height / 2) - (Screen.height / 3.5f));
            DrawButton(m_StartButtonText, startPos, new Vector2((Screen.width / 4), (Screen.height / 9)),
                       ButtonStyle, m_StartButtonColor, Color.clear, BgTexture, delegate()
                       {
                           if ((RequireSetName == true) && (vp_Gameplay.PlayerName == m_DefaultPlayerName))
                           {
                               m_NameColor = Color.red;
                               vp_Timer.In(1, delegate()
                               {
                                   m_NameColor = Color.white;
                               });
                               return;
                           }
                           if(FindObjectOfType<vp_MPConnection>() == null)
                           {
                               m_StartButtonText = "No MP scripts ...";
                               m_StartButtonColor = Color.red;
                               vp_Timer.In(1, delegate()
                               {
                                   m_StartButtonColor = Color.white;
                                   m_StartButtonText = m_DefaultStartButtonText;
                               });
                               return;
                           }
                           m_StartButtonText = "Connecting ...";
				
                           vp_Timer.In(0.1f, delegate()
                           {
                               vp_MPConnection.StayConnected = true;
                               Chat.enabled = true;
                               UnityEngine.GUI.FocusControl("1");
                               m_UserPressedConnect = true;
                               while (vp_Gameplay.PlayerName.Length < 3)
                               {
                                   vp_Gameplay.PlayerName = "#" + vp_Gameplay.PlayerName;
                               }
                           });
                       });

            UnityEngine.GUI.enabled = !m_UserPressedConnect;

            m_Pos.x = startPos.x;
            m_Pos.y += Screen.height / 50;
            DrawTextField("Name: ", m_Pos, new Vector2((Screen.width / 4), (Screen.height / 9)),
                          ButtonStyle, m_NameColor, Color.clear, BgTexture);
            m_Pos.x = startPos.x;
            m_Pos.y += Screen.height / 50;
            DrawButton(m_FullScreenButtonText, m_Pos, new Vector2((Screen.width / 4), (Screen.height / 9)),
                       ButtonStyle, Color.white, Color.clear, BgTexture, delegate()
                       {
                           ToggleFullscreen();
                           m_FullScreenButtonText = (Screen.fullScreen ? "Fullscreen" : "Windowed");
                       });
            m_Pos.x = startPos.x;
            m_Pos.y += Screen.height / 50;
            if(string.IsNullOrEmpty(m_QualityButtonText))
                m_QualityButtonText = "Quality: " + m_Qualitys[QualitySettings.GetQualityLevel()];
            DrawButton(m_QualityButtonText, m_Pos, new Vector2((Screen.width / 4), (Screen.height / 9)),
                       ButtonStyle, Color.white, Color.clear, BgTexture, delegate()
                       {
                           m_QualityButtonText = "Please wait ...";
                           m_Quality++;
                           if (m_Quality > 5)
                               m_Quality = 0;
                           vp_Timer.In(0.1f, delegate()
                           {
                               QualitySettings.SetQualityLevel(m_Quality, false);
                               m_QualityButtonText = "Quality: " + m_Qualitys[QualitySettings.GetQualityLevel()];
                           });
                       });

            m_Pos.x = startPos.x;
            m_Pos.y += Screen.height / 50;
            DrawButton("Quit", m_Pos, new Vector2((Screen.width / 4), (Screen.height / 9)),
                       ButtonStyle, Color.white, Color.clear, BgTexture, delegate()
                       {
                           Application.Quit();
                       });
		
            UnityEngine.GUI.enabled = true;

        }


        /// <summary>
        /// 
        /// </summary>
        protected virtual void ToggleFullscreen()
        {
            if (!Screen.fullScreen)
            {
                Resolution k = new Resolution();
                foreach (Resolution r in Screen.resolutions)
                {
                    if (r.width > k.width)
                    {
                        k.width = r.width;
                        k.height = r.height;
                    }
                }
                Screen.SetResolution(k.width, k.height, true);
            }
            else
                Screen.SetResolution(800, 600, false);
        }


        /// <summary>
        /// globalevent target: returns player name chosen in the main menu
        /// </summary>
        [System.Obsolete("Please use 'vp_Gameplay.PlayerName' instead.")]
        protected virtual string GetPlayerName()
        {
            return vp_Gameplay.PlayerName;
        }
	

        /// <summary>
        /// 
        /// </summary>
        protected virtual void DrawButton(string text, Vector2 position, Vector2 scale, GUIStyle textStyle, Color textColor, Color bgColor, Texture texture, System.Action action)
        {

            if (scale.x == 0)
                scale.x = textStyle.CalcSize(new GUIContent(text)).x;
            if (scale.y == 0)
                scale.y = textStyle.CalcSize(new GUIContent(text)).y;

            m_LabelRect.x = m_Pos.x = position.x;
            m_LabelRect.y = m_Pos.y = position.y;
            m_LabelRect.width = m_Size.x = scale.x;
            m_LabelRect.height = m_Size.y = scale.y;

            if (bgColor != Color.clear)
            {
                UnityEngine.GUI.color = bgColor;
                UnityEngine.GUI.DrawTexture(m_LabelRect, texture);
            }

            UnityEngine.GUI.color = textColor;
            if (UnityEngine.GUI.Button(m_LabelRect, text, textStyle))
                action.Invoke(); 
            UnityEngine.GUI.color = Color.white;

            m_Pos.x += m_Size.x;
            m_Pos.y += m_Size.y;

        }
	

        /// <summary>
        /// 
        /// </summary>
        public void Reset()
        {

            m_StartButtonText = m_DefaultStartButtonText;
            vp_MPConnection.StayConnected = false;

            //Chat.Clear();
            //Chat.enabled = false;

            UnityEngine.GUI.FocusControl("1");
            m_UserPressedConnect = false;
            vp_Utility.LockCursor = false;

        }


        /// <summary>
        /// 
        /// </summary>
        protected virtual void DrawTextField(string text, Vector2 position, Vector2 scale, GUIStyle textStyle, Color textColor, Color bgColor, Texture texture)
        {

            if (scale.x == 0)
                scale.x = textStyle.CalcSize(new GUIContent(text)).x;
            if (scale.y == 0)
                scale.y = textStyle.CalcSize(new GUIContent(text)).y;

            m_LabelRect.x = m_Pos.x = position.x;
            m_LabelRect.y = m_Pos.y = position.y;
            m_LabelRect.width = m_Size.x = scale.x;
            m_LabelRect.height = m_Size.y = scale.y;

            if (bgColor != Color.clear)
            {
                UnityEngine.GUI.color = bgColor;
                UnityEngine.GUI.DrawTexture(m_LabelRect, texture);
            }

            UnityEngine.GUI.color = textColor;
            m_LabelRect.x = position.x + 5 + (m_LabelRect.width * 0.5f);

            vp_Gameplay.PlayerName = UnityEngine.GUI.TextField(m_LabelRect, vp_Gameplay.PlayerName, 16, LeftStyle);
            vp_Gameplay.PlayerName = vp_Gameplay.PlayerName.Replace("\n", "");
            vp_Gameplay.PlayerName = vp_Gameplay.PlayerName.Replace(".", "");
            vp_Gameplay.PlayerName = vp_Gameplay.PlayerName.Replace(",", "");
            vp_Gameplay.PlayerName = vp_Gameplay.PlayerName.Replace("|", "");
            //Event e = Event.current;	// TODO: needed?
            if (Event.current.keyCode == KeyCode.KeypadEnter || Event.current.keyCode == KeyCode.Return)
                m_UserHasHitReturn = true;


            if (m_UserHasHitReturn)
            {
                UnityEngine.GUI.FocusControl("1");
                m_UserHasHitReturn = false;
            }

            m_LabelRect.x = m_Pos.x = position.x - 30;
            UnityEngine.GUI.Button(m_LabelRect, text, ButtonStyle); // name:
            UnityEngine.GUI.color = Color.white;

            m_Pos.x += m_Size.x;
            m_Pos.y += m_Size.y;
            UnityEngine.GUI.SetNextControlName("0");

        }


        /// <summary>
        /// 
        /// </summary>
        void OnJoinedRoom()
        {
            m_UserPressedConnect = false;
        }


        // -------- GUI styles --------

        protected GUIStyle m_ButtonStyle = null;
        public GUIStyle ButtonStyle
        {
            get
            {
                if (m_ButtonStyle == null)
                {
                    m_ButtonStyle = new GUIStyle("Label");
                    m_ButtonStyle.font = BigFont;
                    m_ButtonStyle.alignment = TextAnchor.MiddleCenter;
                    m_ButtonStyle.fontSize = 20;
                    m_ButtonStyle.wordWrap = false;
                    m_ButtonStyle.padding.top = 20;
                }
                return m_ButtonStyle;
            }
        }

        protected GUIStyle m_LeftStyle = null;
        public GUIStyle LeftStyle
        {
            get
            {
                if (m_LeftStyle == null)
                {
                    m_LeftStyle = new GUIStyle("Label");
                    m_LeftStyle.font = BigFont;
                    m_LeftStyle.alignment = TextAnchor.MiddleLeft;
                    m_LeftStyle.fontSize = 20;
                    m_LeftStyle.wordWrap = false;
                    m_LeftStyle.clipping = TextClipping.Overflow;
                    m_LeftStyle.padding.top = 20;
                }
                return m_LeftStyle;
            }
        }

        protected GUIStyle m_RightStyle = null;
        public GUIStyle RightStyle
        {
            get
            {
                if (m_RightStyle == null)
                {
                    m_RightStyle = new GUIStyle("Label");
                    m_RightStyle.font = BigFont;
                    m_RightStyle.alignment = TextAnchor.MiddleRight;
                    m_RightStyle.fontSize = 20;
                    m_RightStyle.wordWrap = false;
                    m_RightStyle.padding.top = 20;
                }
                return m_ButtonStyle;
            }
        }


    }
}
