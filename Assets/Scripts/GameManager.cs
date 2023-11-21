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
        // ������ ���� (FPS 60)
        Application.targetFrameRate = 60;

        donglePool = new List<Dongle>();
        effectPool = new List<ParticleSystem>();

        // ������Ʈ Ǯ ����
        for (int index = 0; index < poolSize; index++)
        {
            MakeDongle();
        }

        // �ִ� ���� ����
        if (!PlayerPrefs.HasKey("MaxScore"))
        {
            PlayerPrefs.SetInt("MaxScore", 0);
        }
        maxScoreText.text = PlayerPrefs.GetInt("MaxScore").ToString();
    }

    public void GameStart()
    {
        // UI ��Ʈ��
        startGroup.SetActive(false);
        line.SetActive(true);
        floor.SetActive(true);
        scoreText.gameObject.SetActive(true);
        maxScoreText.gameObject.SetActive(true);
        // ȿ����
        SfxPlay(Sfx.Button);
        // BGM ����
        bgmPlayer.Play();
        // ���� ���� ����
        Invoke("NextDongle", 1.5f);
    }

    Dongle MakeDongle()
    {
        // ����Ʈ ���� + Ǯ ����
        GameObject instantEffectObj = Instantiate(effectPrefab, effectGroup);
        instantEffectObj.name = "Effect" + effectPool.Count;
        ParticleSystem instantEffect = instantEffectObj.GetComponent<ParticleSystem>();
        effectPool.Add(instantEffect);

        // ���ο� ���� ���� (���� -> Ȱ��ȭ) + Ǯ ����
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

        // ���� �巡��
        lastDongle.Drag();
    }

    public void TouchUp()
    {
        if (lastDongle == null)
            return;

        // ���� ��� (���� ����)
        lastDongle.Drop();
        lastDongle = null;

    }

    public void GameOver()
    {
        // ���� ���� �� ���
        if (isOver)
            return;
        isOver = true;
        bgmPlayer.Stop();
        StartCoroutine(GameOverRoutine());
    }

    IEnumerator GameOverRoutine()
    {
        // 1. ��� �ȿ� Ȱ��ȭ �Ǿ��ִ� ��� ���� ��������
        Dongle[] dongles = FindObjectsOfType<Dongle>();

        // 2. ����� ���� ��� ������ ����ȿ�� ��Ȱ��ȭ
        for (int index = 0; index < dongles.Length; index++)
        {
            dongles[index].rigid.simulated = false;
        }

        // 3. 1���� ����� �ϳ��� �����ؼ� �����
        for (int index = 0; index < dongles.Length; index++)
        {
            dongles[index].Hide(Vector3.up * 100);
            yield return new WaitForSeconds(0.1f);
        }

        yield return new WaitForSeconds(1f);

        // ���� ����
        subScoreText.text = "���� : " + scoreText.text;
        // �ִ� ���� ����
        int maxScore = Mathf.Max(PlayerPrefs.GetInt("MaxScore"), score);
        PlayerPrefs.SetInt("MaxScore", maxScore);
        // UI ����
        endGroup.SetActive(true);
        // ȿ���� ���
        SfxPlay(Sfx.Over);
    }

    public void Reset()
    {
        // ȿ���� ���
        SfxPlay(Sfx.Button);

        StartCoroutine(ResetRoutine());
    }

    IEnumerator ResetRoutine()
    {
        yield return new WaitForSeconds(1f);

        // ��� �ٽ� �ҷ�����
        SceneManager.LoadScene("Main");
    }

    public void SfxPlay(Sfx type)
    {
        // ȿ���� ���� ����
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
        // SFX �÷��̾� Ŀ�� �̵�
        sfxCursor = (sfxCursor + 1) % sfxPlayer.Length;
    }


    private void LateUpdate()
    {
        scoreText.text = score.ToString();
    }
}
