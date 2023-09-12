
 using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayerController : MonoBehaviour
{

    public float speed = 3f;
    string upAnime = "Up";
    string downAnime = "Down";
    string rightAnime = "Right";
    string leftAnime = "Left";
    string deadAnime = "Dead";

    public string nowAnimation = "";
    string oldAnimation = "";
    float axisH;
    float axisV;
    public float angleZ = -90.0f;

    Rigidbody2D rb;
    bool isMoving = false;

    //ダメージ対応
    public static int hp = 4;
    public static string gameState;
    public FixedJoystick joystick;
    bool inDamage = false;



    void Start()
    {
        rb = this.GetComponent<Rigidbody2D>();
        oldAnimation = downAnime;
        gameState = "playing";
        hp = PlayerPrefs.GetInt("PlayerHp");
    }

    void Update()
    {
        //ゲーム中以外は何もしない
        if(gameState != "playing" || inDamage)
        {
            return;
        }
        if(!isMoving)
        {
#if UNITY_EDITOR
        axisH = Input.GetAxisRaw("Horizontal");
        axisV = Input.GetAxisRaw("Vertical");
#elif UNITY_ANDROID
        axisH = joystick.Horizontal;
        axisV = joystick.Vertical;
#endif

        }
        Vector2 fromPos = transform.position;
        Vector2 movePos = new Vector2(fromPos.x + axisH , fromPos.y + axisV);// 0,0,0 transform.位置 x = +-1, & y
        angleZ = GetAngle(fromPos , movePos);

        if(angleZ > -45 && angleZ < 45)
        {
            nowAnimation = rightAnime;
        }
        else if(angleZ >= 45 && angleZ <= 135)
        {
            nowAnimation = upAnime;
        }
        else if(angleZ >= -135 && angleZ <= -45)
        {
            nowAnimation = downAnime;
        }
        else
        {
            nowAnimation = leftAnime;
        }
        //animation切り替え
        if(nowAnimation != oldAnimation)
        {
            oldAnimation = nowAnimation;
            GetComponent<Animator>().Play(nowAnimation);
        }

    }

    void FixedUpdate()
    {
        //ゲーム中以外は何もしない
        if (gameState != "playing")
        {
            return;
        }
        if (inDamage)
        {
            //ダメージ中点滅させる
            float val = Mathf.Sin(Time.time * 50);
            if (val > 0)
            {
                gameObject.GetComponent<SpriteRenderer>().enabled = true;
            }
            else
            {
                gameObject.GetComponent<SpriteRenderer>().enabled = false;
            }
            return; //ダメージ中は操作による移動させない
        }
        rb.velocity = new Vector2(axisH, axisV) * speed;
    }

    public void SetAxis(float h , float v)
    {
        axisH = h;
        axisV = v;
        if(axisH == 0 && axisV == 0)
        {
            isMoving = false;
        }
        else
        {
            isMoving = true;
        }
    }

    float GetAngle(Vector2 p1, Vector2 p2)
    {
        float angle;
        if(axisH != 0 || axisV != 0)
        {
            //移動中であれば角度を更新
            //p1からp2への差分（原点を０にするため）
            float dx = p2.x - p1.x;
            float dy = p2.y - p1.y;
            //アークタンジェント２関数で角度（ラジアン）を求める
            float rad = Mathf.Atan2(dy, dx);// x1 = angle0  (x-1 , y-1) =angel-45 (-y) =angle-90
            //ラジアンを度に変換して返す
            angle = rad * Mathf.Rad2Deg;
        }
        else
        {
            //停止中であれば以前の角度を維持
            angle = angleZ;
        }
        return Mathf.Round(angle);//0.0000001 くらいの誤差が出る
    }
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if(collision.gameObject.tag == "Enemy")
        {
            GetDamage(collision.gameObject);
        }
    }
    //ダメージ
    void GetDamage(GameObject enemy)
    {
        if(gameState == "playing")
        {
            hp--;
            PlayerPrefs.SetInt("PlayerHp", hp);
            if(hp > 0)
            {
                rb.velocity = new Vector2(0, 0);
                Vector3 v = (transform.position - enemy.transform.position).normalized;
                rb.AddForce(new Vector2(v.x*4, v.y*4), ForceMode2D.Impulse);
                inDamage = true;
                Invoke("DamageEnd", 0.25f);
            }
            else
            {
                GameOver();
            }
        }
    }
    void DamageEnd()
    {
        inDamage = false;
        gameObject.GetComponent<SpriteRenderer>().enabled = true;
    }
    void GameOver()
    {
        gameState = "gameover";
        GetComponent<CapsuleCollider2D>().enabled = false;
        rb.velocity = new Vector2(0, 0);
        rb.gravityScale = 1;
        rb.AddForce(new Vector2(0, 5), ForceMode2D.Impulse);
        GetComponent<Animator>().Play(deadAnime);
        Destroy(gameObject, 1f);
    }


}
