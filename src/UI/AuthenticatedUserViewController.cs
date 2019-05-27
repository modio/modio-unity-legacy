using System.Collections;

using UnityEngine;

namespace ModIO.UI
{
    /// <summary>Displays the details of the currently authenticated user.</summary>
    [RequireComponent(typeof(UserView))]
    public class AuthenticatedUserViewController : MonoBehaviour
    {
        // ---------[ FIELDS ]---------
        [Header("Settings")]
        /// <summary>Display data for an unauthenticated user.</summary>
        [SerializeField]
        private UserDisplayData m_guestData = default(UserDisplayData);

        // --- ACCESSORS ---
        /// <summary>The UserView this component controls.</summary>
        public UserView view
        {
            get { return this.gameObject.GetComponent<UserView>(); }
        }

        // ---------[ INITIALIZATION ]---------
        private void Start()
        {
            view.Initialize();

            if(UserAuthenticationData.instance.userId == UserProfile.NULL_ID)
            {
                view.data = this.m_guestData;
            }
            else
            {
                StartCoroutine(FetchUserProfile());
            }
        }

        private IEnumerator FetchUserProfile()
        {
            bool isUnresolvable = false;
            UserProfile profile = null;

            float nextRetrySeconds = 0;
            string errorMessage = null;

            while(!isUnresolvable
                  && profile == null
                  && this != null)
            {
                WebRequestError error = null;
                bool isDone = false;

                ModManager.GetUserProfile(UserAuthenticationData.instance.userId,
                                          (p) => { isDone = true; profile = p; },
                                          (e) => { isDone = true; error = e; } );

                while(!isDone) { yield return null; }

                if(error != null)
                {
                    isUnresolvable = error.isRequestUnresolvable;

                    if(isUnresolvable)
                    {
                        errorMessage = ("Unable to get your profile from the mod.io servers.\n"
                                        + error.displayMessage);
                    }
                    else
                    {
                        yield return new WaitForSeconds(nextRetrySeconds);
                        nextRetrySeconds += 5f;
                    }
                }
                else if(profile == null)
                {
                    errorMessage = ("Unable to get your profile from the mod.io servers.\n"
                                    + "An unknown error has occurred.");
                    isUnresolvable = true;
                }
            }

            if(isUnresolvable)
            {
                MessageSystem.QueueMessage(MessageDisplayData.Type.Error, errorMessage);
            }
            else if(this != null)
            {
                Debug.Assert(profile != null);

                view.DisplayUser(profile);
            }
        }
    }
}
