using UnityEngine;

public class CollidingObject : MonoBehaviour {

    [SerializeField]
    private int price;

    private void OnTriggerEnter2D(Collider2D collision) {
        if(collision.gameObject.CompareTag("Vehicle")) {

            if(gameObject.name.Contains("Fuel")) {  
                GameManager.Instance.FuelCharge();
                gameObject.SetActive(false);
            }
            

            else if(gameObject.name.Contains("Goal")) {  
                GameManager.Instance.ReachGoal = true;
                GameManager.Instance.StartGameOver();
            }


            else if(gameObject.name.Contains("Coin")) {  
                GameManager.Instance.GetCoin(price);
                gameObject.SetActive(false);
            }
        }
    }
}