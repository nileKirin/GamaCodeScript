using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;

public class PlayerManager : MonoBehaviour
{
//Object,Component指定用
    public FixedJoystick joystick;
    public GroundCheck ground;
//MyComponent
    private Rigidbody2D rb;
    private Animator ac;
    private SpriteRenderer sp;
    public Slider specialSlider;

//global変数
    int specialSliderValue;

//攻撃判定
    public Transform attackPoint;
    public float attackRadius;
    public LayerMask enemyLayer;
//待ち時間（ダメージ、攻撃）
    public float waitTimer = 0f;
    public float damageWaitTimer = 1.5f;
//状態のBool
    private bool isAttack = true;
    public bool isDefense;
    public bool buttonDownFlg = false;
    private bool isDodging = false; //うまくいかない　いったん保留　196=216


//キャラ用status
    public int hitPoints = 4;
    public float moveSpeed = 7f;
    public float jumpForce = 360;
    public float dodgeForce = 10f;
    public int attackDamage = 0;

//animationコンデションtrigger
    string animeVelocityXFloat = "velocityX";
    string animeVelocityYFloat = "velocityY";
    string animeAttacksTrigger = "attacks";
    string animeAttackTypeInt = "attackType";
    string animeJumpBool = "jump";
    string animeDamageTrigger = "takeDamage";
    string animeDeadTrigger = "deadEnd";
    string animeDefenseBool = "defense";

//groundChecker
    private bool isGround = false;

//キーコード用
    public  KeyCode jumpKey = KeyCode.Space;
    public  KeyCode defenseKey = KeyCode.G;
    public  KeyCode attackNormalKey = KeyCode.Q;
    public  KeyCode attackSpecialKey = KeyCode.W;
    public  KeyCode attackRoyalKey = KeyCode.E;
    public  KeyCode dodgeKey = KeyCode.T;


    public event Action OnDeadCalled;


    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        ac = GetComponent<Animator>();
        sp = GetComponent<SpriteRenderer>();
        specialSliderValue = 0;
        OnDeadCalled += HandleOnDead;
    }

/*
保留：回避、連続攻撃、void EventAnimeClipIsAttackTrue()..ダメージ判定を合わせる？うまくやる（enemy,Boss）
進行ボス


*/

    void Update()
    {

        waitTimer -= Time.deltaTime;//ダメージ判定用
        isGround = ground.IsGround();//地面拝呈
        ActionEvent();//PC キー別の処理
        AnimationApply(rb.velocity.x, rb.velocity.y);//animationまとめ
        isDamageWait();//ダメージ後の無敵
        Movement();//移動
        specialSlider.value = specialSliderValue;
    }

    private void ActionEvent()
    {
        if (Input.GetKeyDown(jumpKey))
        {
            //jumpAction?.Invoke();
            Jump();
        }
            else if(Input.GetKeyDown(dodgeKey) && !isDodging)
            {
                Dodge();
            }
        else if (Input.GetKeyDown(defenseKey))
        {
            moveSpeed = 0;
            if(isGround && isAttack)
            {
                isDefense = true;
                ac.SetBool(animeDefenseBool, true);
            }
        }
        else if (Input.GetKeyUp(defenseKey))
        {
            moveSpeed = 7;
            isDefense = false;
            ac.SetBool(animeDefenseBool, false);
        }
        else if (Input.GetKeyDown(attackNormalKey))
        {
            AttackNormal();
        }
        else if (Input.GetKeyDown(attackSpecialKey))
        {
            AttackSpecial();
        }
        else if (Input.GetKeyDown(attackRoyalKey))
        {
            AttackRoyal();
        }
    }

    public void Dodge()
    {
        Debug.Log("関数");
        isDodging = true;
        //rb.velocity.x = new Vector2(rb.velocity.x * dodgeForce, rb.velocity.y);
        //rb.velocity = new Vector2(rb.velocity.x * dodgeForce, rb.velocity.y);
        rb.AddForce(new Vector2(dodgeForce, 0f), ForceMode2D.Impulse);
        Physics2D.IgnoreLayerCollision(8, 9, true);// 敵オブジェクトの当たり判定を　true無視する: false 当たる
        StartCoroutine(ResetDodgeState());
    }

    private IEnumerator ResetDodgeState()
    {
        Debug.Log("コルーチン");
        yield return new WaitForSeconds(1f);
        Physics2D.IgnoreLayerCollision(8, 9, false);
        isDodging = false;
    }

    public void Jump()
    {
        if(isGround && isAttack)
        {
            this.rb.AddForce(transform.up* jumpForce);
        }
    }

    public void GuardHold()
    {
        moveSpeed = 0;
        if(isGround && isAttack)
        {
            isDefense = true;
            ac.SetBool(animeDefenseBool, true);
            buttonDownFlg = true;
        }
    }
    public void GuardEnd()
    {
        moveSpeed = 7;
        isDefense = false;
        ac.SetBool(animeDefenseBool, false);
        buttonDownFlg = false;
    }

    /*攻撃パターン3種*/
　//ApplyDamageToEnemies()//ダメージ判定 animationClipにevent付与してます
    public void AttackNormal()
    {
        if(isAttack && isGround)
        {
            isAttack = false; //animation の最後でtrueを呼び出す(falseは攻撃中、移動速度半減、行動制限)
            attackDamage = 2;
            AnimationAttack(AttackType.TypeNormalAttack);
        }
    }

    public void AttackSpecial()
    {
        if(isAttack && isGround)
        {
            isAttack = false;
            attackDamage = 3;
            AnimationAttack(AttackType.TypeSpecialAttack);
        }
    }
    public void AttackRoyal()
    {
        if(isAttack && isGround && specialSliderValue >= 100)
        {
            isAttack = false;
            specialSliderValue = 0;
            attackDamage = 20;
            AnimationAttack(AttackType.TypeRoyalAttack);
        }
    }

    public void EventAnimeClipIsAttackTrue()//この長い名前のメソッドは自分への罰、、、
    {
        isAttack = true;
    }
    public void EventAnimeClipApplyDamageToEnemy()
    {
        Collider2D[] hitEnemys = Physics2D.OverlapCircleAll(attackPoint.position, attackRadius, enemyLayer);
        foreach (Collider2D hitEnemy in hitEnemys)
        {
        /*
            EnemyManager enemyManager = hitEnemy.GetComponent<EnemyManager>();
            enemyManager.OnDamage(attackDamage);
        */
            BossManager bossManager = hitEnemy.GetComponent<BossManager>();
            bossManager.OnDamage(attackDamage);
            specialSliderValue += 10;
        }
    }

//敵キャラが呼び出す為のダメージメソッド
    public void OnDamage()
    {
        if(!isDefense)
        {
            if(waitTimer <= 0f)
            {
                waitTimer = damageWaitTimer;
                this.hitPoints -= 1;
                ac.SetTrigger(animeDamageTrigger);
                Debug.Log("playerのHPは残り" + this.hitPoints + "です");
                if(this.hitPoints <= 0)
                {
                    waitTimer = 0;
                    OnDeadCalled?.Invoke();
                    Destroy(gameObject , 2f);
                    ac.SetTrigger(animeDeadTrigger);
                }
                isAttack = true;
            }
        }

    }
//OnDamage呼ばれるとwaitTimerが正になる
    void isDamageWait()
    {
        if(waitTimer > 0){
            sp.color = new Color(1, 1, 1, sp.color.a == 0 ? 1 : 0);
 // （点灯）（消灯）を切り替える
        }
        else
        {
            sp.color = new Color(1,1,1,1);
        }
    }
    private void HandleOnDead()
    {
        moveSpeed = 0;
        isAttack = false;
    }


//攻撃判定
    private void OnDrawGizmosSelected() {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(attackPoint.position,attackRadius);
    }

//移動
    void Movement()
    {
#if UNITY_EDITOR
        float x = Input.GetAxisRaw("Horizontal");//方向キー 右（１）左(-1)が出力される float x = 1 or-1 or null;
        //float y = Input.GetAxisRaw("Vertical");//方向キー ↑（１）下(-1)が出力される
        rb.velocity = isAttack ? new Vector2(x * moveSpeed, rb.velocity.y) : new Vector2(x * moveSpeed/2.5f, rb.velocity.y);
#elif UNITY_ANDROID
        float x = joystick.Horizontal;
        //float y = joystick.Vertical;
        rb.velocity = isAttack ? new Vector2(x * moveSpeed, rb.velocity.y) : new Vector2(x * moveSpeed/2.5f, rb.velocity.y);
#endif
        if(rb.velocity.x > 0)
        {
            transform.localScale = new Vector3(1,1,1);
        }
        if(rb.velocity.x < 0)
        {
            transform.localScale = new Vector3(-1,1,1);
        }
    }


//Anime用
    public void AnimationApply(float velocityX, float VelocityY)
    {
        ac.SetFloat(animeVelocityXFloat,Mathf.Abs(velocityX));
        if(isGround != true)//falseならば（地面にいるとtrue）
        {
            ac.SetBool(animeJumpBool, true);//ジャンプ判定をtrue
            ac.SetFloat(animeVelocityYFloat, VelocityY);//animeParameterに引数Yを代入
        }
        else
        {
            ac.SetBool(animeJumpBool, false);//ジャンプ判定フォルス
        }
    }

    public void AnimationAttack(AttackType typeId)
    {
        ac.SetInteger(animeAttackTypeInt, (int)typeId);
        ac.SetTrigger(animeAttacksTrigger);
    }

    public enum AttackType : int
    {
        TypeNormalAttack = 1,
        TypePowerAttack = 2,
        TypeSpecialAttack = 3,
        TypeRoyalAttack = 4
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if(collision.gameObject.tag == "HpHeal")
        {
            this.hitPoints += 1;
            Destroy(collision.gameObject);
        }
    }
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if(collision.gameObject.tag == "CheckPoint")
        {
            Debug.Log("チェックポイント");
        }
    }

}
