using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    [Header(" ----------- [ Core ]")]
    public int score;
    public int maxLevel;
    public bool isOver;

    [Header(" ----------- [ Object Pooling ]")]
    public GameObject donglePrefab;
    public Transform dongleGroup;
    public GameObject effectPrefab;
    public Transform effectGroup;
    [Range(1, 30)]
    public int poolSize;
    public List<Dongle> donglePool;
    public List<ParticleSystem> effectPool;
    public Dongle lastDongle;
    public int poolCursor;

    [Header(" ------------- [ Audio ]")]
    public AudioSource bgmPlayer;
    public AudioSource[] sfxPlayer;
    public AudioClip[] sfxClip;
    int sfxCursor;

    [Header(" ------------- [ UI ]")]
    public GameObject line;
    public GameObject floor;
    public GameObject startGroup;
    public GameObject endGroup;
    public Text scoreText;
    public Text maxScoreText;
    public Text subScoreText;

    public enum Sfx { levelUp, Next, Attach, Button, Over };

    private void Awake()
    {
        // 프레임 설정 (FPS 60)
        Application.targetFrameRate = 60;

        donglePool = new List<Dongle>();
        effectPool = new List<ParticleSystem>();

        // 오브젝트 풀 시작
        for (int index = 0; index < poolSize; index++)
        {
            MakeDongle();
        }

        // 최대 점수 설정
        if (!PlayerPrefs.HasKey("MaxScore"))
        {
            PlayerPrefs.SetInt("MaxScore", 0);
        }
        maxScoreText.text = PlayerPrefs.GetInt("MaxScore").ToString();
    }

    public void GameStart()
    {
        // UI 컨트롤
        startGroup.SetActive(false);
        line.SetActive(true);
        floor.SetActive(true);
        scoreText.gameObject.SetActive(true);
        maxScoreText.gameObject.SetActive(true);
        // 효과음
        SfxPlay(Sfx.Button);
        // BGM 시작
        bgmPlayer.Play();
        // 동글 생성 시작
        Invoke("NextDongle", 1.5f);
    }

    Dongle MakeDongle()
    {
        // 이펙트 생성 + 풀 저장
        GameObject instantEffectObj = Instantiate(effectPrefab, effectGroup);
        instantEffectObj.name = "Effect" + effectPool.Count;
        ParticleSystem instantEffect = instantEffectObj.GetComponent<ParticleSystem>();
        effectPool.Add(instantEffect);

        // 새로운 동글 생성 (생성 -> 활성화) + 풀 저장
        GameObject instantDongleObj = Instantiate(donglePrefab, dongleGroup);
        instantDongleObj.name = "Dongle" + donglePool.Count;
        Dongle instantDongle = instantDongleObj.GetComponent<Dongle>();
        instantDongle.manager = this;
        instantDongle.effect = instantEffect;
        donglePool.Add(instantDongle);

        return instantDongle;
    }

    Dongle GetDongle()
    {
        for (int index = 0; index < donglePool.Count; index++)
        {
            poolCursor = (poolCursor + 1) % donglePool.Count;
            if (!donglePool[poolCursor].gameObject.activeSelf)
            {
                return donglePool[poolCursor];
            }
        }

        return MakeDongle();
    }

    void NextDongle()
    {
        if (isOver)
            return;

        lastDongle = GetDongle();
        lastDongle.level = Random.Range(0, maxLevel);
        lastDongle.gameObject.SetActive(true);

        SfxPlay(Sfx.Next);
        StartCoroutine(WaitNext());
    }

    IEnumerator WaitNext()
    {
        while (lastDongle != null)
        {
            yield return null;
        }

        yield return new WaitForSeconds(0.5f);

        NextDongle();
    }

    public void TouchDown()
    {
        if (lastDongle == null)
            return;

        // 동글 드래그
        lastDongle.Drag();
    }

    public void TouchUp()
    {
        if (lastDongle == null)
            return;

        // 동글 드랍 (변수 비우기)
        lastDongle.Drop();
        lastDongle = null;

    }

    public void GameOver()
    {
        // 게임 오버 및 결산
        if (isOver)
            return;
        isOver = true;
        bgmPlayer.Stop();
        StartCoroutine(GameOverRoutine());
    }

    IEnumerator GameOverRoutine()
    {
        // 1. 장면 안에 활성화 되어있는 모든 동글 가져오기
        Dongle[] dongles = FindObjectsOfType<Dongle>();

        // 2. 지우기 전에 모든 동글의 물리효과 비활성화
        for (int index = 0; index < dongles.Length; index++)
        {
            dongles[index].rigid.simulated = false;
        }

        // 3. 1번의 목록을 하나씩 접근해서 지우기
        for (int index = 0; index < dongles.Length; index++)
        {
            dongles[index].Hide(Vector3.up * 100);
            yield return new WaitForSeconds(0.1f);
        }

        yield return new WaitForSeconds(1f);

        // 점수 적용
        subScoreText.text = "점수 : " + scoreText.text;
        // 최대 점수 갱신
        int maxScore = Mathf.Max(PlayerPrefs.GetInt("MaxScore"), score);
        PlayerPrefs.SetInt("MaxScore", maxScore);
        // UI 띄우기
        endGroup.SetActive(true);
        // 효과음 출력
        SfxPlay(Sfx.Over);
    }

    public void Reset()
    {
        // 효과음 출력
        SfxPlay(Sfx.Button);

        StartCoroutine(ResetRoutine());
    }

    IEnumerator ResetRoutine()
    {
        yield return new WaitForSeconds(1f);

        // 장면 다시 불러오기
        SceneManager.LoadScene("Main");
    }

    public void SfxPlay(Sfx type)
    {
        // 효과음 사운드 지정
        switch (type)
        {
            case Sfx.levelUp:
                sfxPlayer[sfxCursor].clip = sfxClip[Random.Range(0, 3)];
                break;
            case Sfx.Next:
                sfxPlayer[sfxCursor].clip = sfxClip[3];
                break;
            case Sfx.Attach:
                sfxPlayer[sfxCursor].clip = sfxClip[4];
                break;
            case Sfx.Button:
                sfxPlayer[sfxCursor].clip = sfxClip[5];
                break;
            case Sfx.Over:
                sfxPlayer[sfxCursor].clip = sfxClip[6];
                break;
        }

        sfxPlayer[sfxCursor].Play();
        // SFX 플레이어 커서 이동
        sfxCursor = (sfxCursor + 1) % sfxPlayer.Length;
    }


    private void LateUpdate()
    {
        scoreText.text = score.ToString();
    }
}
