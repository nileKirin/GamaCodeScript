using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class BossManager : MonoBehaviour
{
    public EnemyAiState currentState;
    private Rigidbody2D rb;
    private Animator ac;
    public GameObject damagePrefab;
    public Transform canvasTrans;

// ENEMY STATUS
    public int hitPoints = 10;
    public float moveSpeed = 5f;
// AI 変数
    float waitTimer;
    public float attackWaitDuration = 1.5f;
    public float damageWaitDuration = 0.7f;
    float moveDistance; // ランダムな移動距離用
    float moveDirection; // ランダムな方向用
    public float minMoveDistance = 7f;
    public float maxMoveDistance = 8f;
    public float stopDistance = 1.5f;
// 視界用変数
    public Transform visionPoint;
    public float visionRadius;
    public Transform attackPoint;
    public float attackRadius;
    public LayerMask playerLayer;
//Animate Parameter
    string animeVelocityXFloat = "velocityX";
    string animeAttackTypeInt = "attackType";
    string animeAttacksTrigger = "attacks";
    string animeDamageTrigger = "damage";
    string animeDeadTrigger = "dead";
    bool isFocusMove = false;
    bool isFocusAttack = false;
    bool isDamage = false;
//ダメージテキスト用
    float damageTextDuration = 0.2f; // 消滅までの時間


    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        ac = GetComponent<Animator>();
        moveDistance = UnityEngine.Random.Range(minMoveDistance, maxMoveDistance);
        moveDirection = UnityEngine.Random.Range(0, 2) == 0 ? -1f : 1f;
    }


    void Update()
    {
        PlayerFocus();
         switch (currentState)
        {
            case EnemyAiState.WAIT:
                if (waitTimer <= 0f && !isFocusMove) //focus = False
                {
                    waitTimer = 2f;
                    currentState = EnemyAiState.MOVE;
                }
                else if(waitTimer <= 0f && isFocusMove)//focus = true
                {
                    currentState = EnemyAiState.FOCUS;
                }
                else
                {
                    isDamage = false;
                    waitTimer -= Time.deltaTime;
                }
                break;

            case EnemyAiState.MOVE:
                if (moveDistance <= 0f && !isFocusMove)
                {
                    moveDistance = UnityEngine.Random.Range(minMoveDistance, maxMoveDistance);
                    moveDirection = UnityEngine.Random.Range(0, 2) == 0 ? -1f : 1f;
                    currentState = EnemyAiState.WAIT;
                }
                else if(isFocusMove)
                {
                    moveDistance = 0;
                    currentState = EnemyAiState.FOCUS;
                }
                else
                {
                    rb.velocity = new Vector2(moveSpeed * moveDirection, rb.velocity.y);
                    moveDistance -= Mathf.Abs(rb.velocity.x);
                }
                break;

            case EnemyAiState.FOCUS:
                if(isFocusMove && isFocusAttack && !isDamage)
                {
                    currentState = EnemyAiState.ATTACK;
                }
                else if(isDamage)
                {
                    currentState = EnemyAiState.DAMAGE;
                }
                else
                {
                    ChasePlayer();
                    //関数にcurrentState = EnemyAiState.WAIT;
                }
                break;
            case EnemyAiState.ATTACK:
                if(!isDamage){
                    AttackPlayer();
                    waitTimer += attackWaitDuration;
                    currentState = EnemyAiState.WAIT;
                }
                else
                {
                    currentState = EnemyAiState.DAMAGE;
                }
                break;
            case EnemyAiState.DAMAGE:
                ac.SetTrigger(animeDamageTrigger);
                waitTimer += damageWaitDuration;
                currentState = EnemyAiState.WAIT;
                break;
            case EnemyAiState.DEAD:
                ac.SetTrigger(animeDeadTrigger);
                Physics2D.IgnoreLayerCollision(9, 8, true);
                Destroy(gameObject, 2f);
                break;
            default:
                Debug.Log("currentState消えた");
                break;
        }
        ac.SetFloat(animeVelocityXFloat, Mathf.Abs(rb.velocity.x));
        if (rb.velocity.x > 0)
        {
            transform.localScale = new Vector3(-1, 1, 1);
        }
        if (rb.velocity.x < 0)
        {
            transform.localScale = new Vector3(1, 1, 1);
        }
    }



    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(visionPoint.position, visionRadius);
        Gizmos.DrawWireSphere(attackPoint.position, attackRadius);
    }

//視界に入ったら攻撃判定、追跡判定のTrueとFalseを判定する。

    void PlayerFocus()
    {
        Collider2D[] lookPlayers = Physics2D.OverlapCircleAll(visionPoint.position, visionRadius, playerLayer);
        Collider2D[] attackPlayers = Physics2D.OverlapCircleAll(attackPoint.position, attackRadius, playerLayer);
        if (lookPlayers.Length > 0 && attackPlayers.Length > 0)
        {
            isFocusMove = true;
            isFocusAttack = true;
        }
        else if(lookPlayers.Length > 0 && attackPlayers.Length == 0)
        {
            isFocusMove = true;
            isFocusAttack = false;
        }
        else{
            isFocusMove = false;
            isFocusAttack = false;
        }
    }

    void ChasePlayer()
    {
        Collider2D[] lookPlayers = Physics2D.OverlapCircleAll(visionPoint.position, visionRadius, playerLayer);
        if (lookPlayers.Length > 0)//視界に入ったら
        {
            foreach(Collider2D lookPlayer in lookPlayers)
            {
                Transform playerTransform = lookPlayer.transform;
                float targetPos = playerTransform.position.x;
                float currentPos = rb.position.x;
                if (Mathf.Abs(targetPos - currentPos) > stopDistance)
                {
                    float moveDirection = targetPos - currentPos;
                    float deceleration = 3f;
                    float maxSpeed = 4f;
//追跡移動制限
                    float smoothMoveDirection = Mathf.Lerp(rb.velocity.x, moveDirection, Time.deltaTime * deceleration);
//｛指定の割合に変える｝Mathf.Lerp(float str,float end , (0~1)//1は100%)str0 end10 ,0.5 なら//5が帰ってくる
                    smoothMoveDirection = Mathf.Clamp(smoothMoveDirection, -maxSpeed, maxSpeed);
//｛速度制限｝Mathf.Clamp(値,最大、最低) 値が最大、最小で止まる
                    rb.velocity = new Vector2(smoothMoveDirection, rb.velocity.y);
                }
            }
        }
        else
        {
            currentState = EnemyAiState.WAIT;
        }
    }



//攻撃モーション用 判定用 ダメージ適応はApplyDamageToPlayer。
    void AttackPlayer()
    {
        rb.velocity = Vector2.zero;
        Collider2D[] attackPlayers = Physics2D.OverlapCircleAll(attackPoint.position, attackRadius, playerLayer);
        if(attackPlayers.Length > 0)
        {
            AnimationAttack(AttackType.TypePowerAttack);
        }
    }
    //animation Event でダメージ playerMManagerのOnDamage
    public void EventAnimeClipApplyDamagePlayer()
    {
        Collider2D[] attackPlayers = Physics2D.OverlapCircleAll(attackPoint.position, attackRadius, playerLayer);
        foreach(Collider2D attackPlayer in attackPlayers)
        {
            attackPlayer.gameObject.GetComponent<PlayerManager>().OnDamage();
        }
    }
//他のクラスで呼び出して this.hitPointsを減らす。
    public void OnDamage(int damage)
    {
        if (this.hitPoints <= 0)
        {
            currentState = EnemyAiState.DEAD;
        }
        else
        {
            isDamage = true;
            rb.velocity = Vector2.zero;
            this.hitPoints -= damage;
            StartCoroutine(SpawnTextAndDestroy(damage));
            currentState = EnemyAiState.DAMAGE;

        }
    }

    private IEnumerator SpawnTextAndDestroy(int damage)
    {
        float offsetX = UnityEngine.Random.Range(-1f, 1f);
        float offsetY = UnityEngine.Random.Range(0.5f, 1.5f);
        Vector3 spawnPosition = transform.position + Vector3.up + Vector3.right * offsetX + Vector3.forward * offsetY;
        GameObject instance = Instantiate(damagePrefab, spawnPosition, Quaternion.identity, canvasTrans);
        TextMeshProUGUI damageText = instance.GetComponentInChildren<TextMeshProUGUI>();
        damageText.text = damage.ToString();
        yield return new WaitForSeconds(damageTextDuration);
        Destroy(instance);
    }

    public enum EnemyAiState
    {
        WAIT,
        MOVE,
        FOCUS,
        ATTACK,
        DAMAGE,
        DEAD
    }

    void AnimationAttack(AttackType typeId)
    {
        ac.SetInteger(animeAttackTypeInt, (int)typeId);
        ac.SetTrigger(animeAttacksTrigger);
    }
    enum AttackType : int
    {
        TypeNormalAttack = 0,
        TypePowerAttack = 1
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if(collision.gameObject.tag == "Player")
        {

        }
    }
}
