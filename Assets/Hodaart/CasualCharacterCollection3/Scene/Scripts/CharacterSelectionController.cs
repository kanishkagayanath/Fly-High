using System.Collections;
using UnityEngine;


namespace CasualCharacterCollection3
{
    public class CharacterSelectionController : MonoBehaviour
    {
        [SerializeField] private GameObject[] characters;
        [SerializeField] private GameObject[] characterUIs;

        private int currentIndex = 0;
        private Animator currentAnimator;
        private Animator facialAnimator;

        [SerializeField] private float switchDelay = 0.5f;
        private bool isSwitching = false;
        void Start()
        {
            ShowCharacter(currentIndex);
        }

        void ShowCharacter(int index)
        {
            for (int i = 0; i < characters.Length; i++)
            {
                characters[i].SetActive(i == index);

                if (characterUIs != null && characterUIs.Length > i)
                    characterUIs[i].SetActive(i == index);
            }

            currentAnimator = characters[index].GetComponent<Animator>();
            facialAnimator = characters[index].GetComponentInChildren<FacialAnimatorMarker>(true)?.GetComponent<Animator>();
        }

        public void NextCharacter()
        {
            StartCoroutine(SwitchCharacterWithDelay(+1));
        }

        public void PreviousCharacter()
        {
            StartCoroutine(SwitchCharacterWithDelay(-1));
        }


        IEnumerator SwitchCharacterWithDelay(int direction)
        {
            if (isSwitching) yield break;
            isSwitching = true;

            PlayIdle();
            PlayBase();
            yield return new WaitForSeconds(switchDelay);

            currentIndex += direction;
            if (currentIndex >= characters.Length) currentIndex = 0;
            if (currentIndex < 0) currentIndex = characters.Length - 1;

            ShowCharacter(currentIndex);

            isSwitching = false;
        }
        // ====================== Animations ======================

        public void PlayWalk1()
        {
            if (currentAnimator == null) return;
            PlayIdle();
            currentAnimator.SetBool("Walk 1", true);
        }

        public void PlayRun1()
        {
            if (currentAnimator == null) return;
            PlayIdle();
            currentAnimator.SetBool("Run 1", true);
        }
        public void PlayWalk2()
        {
            if (currentAnimator == null) return;
            PlayIdle();
            currentAnimator.SetBool("Walk 2", true);
        }

        public void PlayRun2()
        {
            if (currentAnimator == null) return;
            PlayIdle();
            currentAnimator.SetBool("Run 2", true);
        }
        public void PlayIdle2()
        {
            if (currentAnimator == null) return;
            PlayIdle();
            currentAnimator.SetBool("Idle 2", true);
        }
        public void PlayGreet()
        {
            if (currentAnimator == null) return;
            PlayIdle();
            currentAnimator.SetBool("Greet", true);
        }

        public void PlayJump()
        {
            if (currentAnimator == null) return;
            PlayIdle();
            currentAnimator.SetTrigger("Jump");
        }

        public void PlayIdle()
        {
            if (currentAnimator == null) return;
            currentAnimator.SetBool("Walk 1", false);
            currentAnimator.SetBool("Run 1", false);
            currentAnimator.SetBool("Walk 2", false);
            currentAnimator.SetBool("Run 2", false);
            currentAnimator.SetBool("Greet", false);
            currentAnimator.SetBool("Idle 2", false);
        }
        public void PlayAngry01()
        {
            if (facialAnimator == null) return;
            PlayBase();
            facialAnimator.SetBool("Angry 01", true);
        }
        public void PlayAngry02()
        {
            if (facialAnimator == null) return;
            PlayBase();
            facialAnimator.SetBool("Angry 02", true);
        }
        public void PlaySmile01()
        {
            if (facialAnimator == null) return;
            PlayBase();
            facialAnimator.SetBool("Smile 01", true);
        }
        public void PlaySmile02()
        {
            if (facialAnimator == null) return;
            PlayBase();
            facialAnimator.SetBool("Smile 02", true);
        }
        public void PlaySrprised01()
        {
            if (facialAnimator == null) return;
            PlayBase();
            facialAnimator.SetBool("Surprised 01", true);
        }
        public void PlaySad01()
        {
            if (facialAnimator == null) return;
            PlayBase();
            facialAnimator.SetBool("Sad 01", true);
        }
        public void PlaySad02()
        {
            if (facialAnimator == null) return;
            PlayBase();
            facialAnimator.SetBool("Sad 02", true);
        }
        public void PlayTalk()
        {
            if (facialAnimator == null) return;
            PlayBase();
            facialAnimator.SetBool("Talk", true);
        }
        public void PlayBlink()
        {
            if (facialAnimator == null) return;
            PlayBase();
            facialAnimator.SetTrigger("Blink");
        }
        public void PlayBase()
        {
            if (facialAnimator == null) return;
            facialAnimator.SetBool("Angry 01", false);
            facialAnimator.SetBool("Angry 02", false);
            facialAnimator.SetBool("Smile 01", false);
            facialAnimator.SetBool("Smile 02", false);
            facialAnimator.SetBool("Surprised 01", false);
            facialAnimator.SetBool("Sad 01", false);
            facialAnimator.SetBool("Sad 02", false);
            facialAnimator.SetBool("Talk", false);
        }


    }
}
