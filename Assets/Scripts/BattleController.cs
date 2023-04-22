using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BattleController : MonoBehaviour
{
    public static BattleController instance;

    private void Awake()
    {
        instance = this;
    }

    public int startingMana = 4, maxMana = 12;
    public int playerMana, enemyMana;
    private int currentPlayerMaxMana, currentEnemyMaxMana;

    public int startingCardsAmount = 5;
    public int cardsToDrawPerTurn = 2;

    public enum TurnOrder
    {
        playerActive,
        playerCardAttacks,
        enemyActive,
        enemyCardAttacks
    }
    public TurnOrder currentPhase;

    public Transform discardPoint;

    public int playerHealth, enemyHealth;
    public bool battleEnded;

    public float resultScreenDelayTime = 1f;

    [Range(0f, 1f)]
    public float playerFirstChance = .5f;

    public float lowerHealthSpeed = .1f;

    // Start is called before the first frame update
    void Start()
    {
        currentPlayerMaxMana = startingMana;
        currentEnemyMaxMana = startingMana;
        FillPlayerMana();
        FillEnemyMana();

        DeckController.instance.DrawMultipleCards(startingCardsAmount);

        UIController.instance.SetPlayerHealthText(playerHealth);
        UIController.instance.SetEnemyHealthText(enemyHealth);

        if(Random.value > playerFirstChance)
        {
            currentPhase = TurnOrder.playerCardAttacks;
            AdvanceTurn();
        } else
        {
            currentPhase = TurnOrder.enemyCardAttacks;
            AdvanceTurn();
        }

        AudioManager.instance.PlayBGM();
    }

    

    // Update is called once per frame
    void Update()
    {
        //Test code to advance turn using T button
        /*if(Input.GetKeyDown(KeyCode.T))
        {
            AdvanceTurn();
        }*/
    }

    public void SpendPlayerMana(int amountToSpend)
    {
        playerMana = playerMana - amountToSpend;

        //Just in case the mana somehow goes below zero
        if(playerMana < 0)
        {
            playerMana = 0;
        }

        UIController.instance.SetPlayerManaText(playerMana);
    }

    public void FillPlayerMana()
    {
        playerMana = currentPlayerMaxMana;
        UIController.instance.SetPlayerManaText(playerMana);
    }

    public void SpendEnemyMana(int amountToSpend)
    {
        enemyMana -= amountToSpend;

        if(enemyMana < 0)
        {
            enemyMana = 0;
        }

        UIController.instance.SetEnemyManaText(enemyMana);
    }

    public void FillEnemyMana()
    {
        enemyMana = currentEnemyMaxMana;
        UIController.instance.SetEnemyManaText(enemyMana);
    }

    public void AdvanceTurn()
    {
        if(battleEnded == false)
        {
            currentPhase++;

            //Check if currentPhase needs to loop back around to playerActive
            if ((int)currentPhase >= System.Enum.GetValues(typeof(TurnOrder)).Length)
            {
                currentPhase = 0;
            }

            switch (currentPhase)
            {
                case TurnOrder.playerActive:

                    UIController.instance.endTurnButton.SetActive(true);
                    UIController.instance.drawCardButton.SetActive(true);

                    if (currentPlayerMaxMana < maxMana)
                    {
                        currentPlayerMaxMana++;
                    }

                    FillPlayerMana();

                    DeckController.instance.DrawMultipleCards(cardsToDrawPerTurn);

                    break;

                case TurnOrder.playerCardAttacks:

                    //Debug.Log("Skipping player card attacks");
                    //AdvanceTurn();

                    CardPointsController.instance.PlayerAttack();

                    break;

                case TurnOrder.enemyActive:

                    //Debug.Log("Skipping enemy active");
                    //AdvanceTurn();

                    if (currentEnemyMaxMana < maxMana)
                    {
                        currentEnemyMaxMana++;
                    }

                    FillEnemyMana();

                    //TODO: have enemy draw multiple cards at the start of each turn
                    //DeckController.instance.DrawMultipleCards(cardsToDrawPerTurn);

                    EnemyController.instance.StartAction();

                    break;

                case TurnOrder.enemyCardAttacks:

                    //Debug.Log("Skipping enemy card attacks");
                    //AdvanceTurn();

                    CardPointsController.instance.EnemyAttack();

                    break;
            }
        }
       
    }

    public void EndPlayerTurn()
    {
        UIController.instance.endTurnButton.SetActive(false);
        UIController.instance.drawCardButton.SetActive(false);

        AdvanceTurn();
    }

    public void DamagePlayer(int damageAmount)
    {
        if(playerHealth > 0 || battleEnded == false)
        {
            int curHealth = playerHealth;
            curHealth -= damageAmount;

            if(playerHealth <= 0)
            {
                playerHealth = 0;

                //End Battle
                EndBattle();
            }

            StartCoroutine(LowerPlayerHealthSlow(damageAmount));

            //UIController.instance.SetPlayerHealthText(playerHealth);

            UIDamageIndicator damageClone = Instantiate(UIController.instance.playerDamage, UIController.instance.playerDamage.transform.parent);
            damageClone.damageText.text = damageAmount.ToString();
            damageClone.gameObject.SetActive(true);

            AudioManager.instance.PlaySFX(6);
        }

    }

    IEnumerator LowerPlayerHealthSlow(int damageAmount)
    {
        for (int i = 0; i < damageAmount; i++)
        {
            playerHealth -= 1;
            UIController.instance.SetPlayerHealthText(playerHealth);
            yield return new WaitForSeconds(lowerHealthSpeed);
        }
    }

    public void DamageEnemy(int damageAmount)
    {
        if (enemyHealth > 0 || battleEnded == false)
        {
            int curHealth = enemyHealth;
            curHealth -= damageAmount;

            if (curHealth <= 0)
            {
                enemyHealth = 0;

                //End Battle
                EndBattle();
            }

            StartCoroutine(LowerEnemyHealthSlow(damageAmount));

            //UIController.instance.SetEnemyHealthText(enemyHealth);

            UIDamageIndicator damageClone = Instantiate(UIController.instance.enemyDamage, UIController.instance.enemyDamage.transform.parent);
            damageClone.damageText.text = damageAmount.ToString();
            damageClone.gameObject.SetActive(true);

            AudioManager.instance.PlaySFX(5);
        }
    }

    IEnumerator LowerEnemyHealthSlow(int damageAmount)
    {
        for(int i=0; i < damageAmount; i++)
        {
            enemyHealth -= 1;
            UIController.instance.SetEnemyHealthText(enemyHealth);
            yield return new WaitForSeconds(lowerHealthSpeed);
        }
    }

    void EndBattle()
    {
        battleEnded = true;
        HandController.instance.EmptyHand();

        if(enemyHealth <= 0)
        {
            UIController.instance.battleResultText.text = "YOU WON!";

            foreach(CardPlacePoint point in CardPointsController.instance.enemyCardPoints)
            {
                if(point.activeCard != null)
                {
                    point.activeCard.MoveToPoint(discardPoint.position, point.activeCard.transform.rotation);
                }
            }

        } else
        {
            UIController.instance.battleResultText.text = "YOU LOSE!";

            foreach (CardPlacePoint point in CardPointsController.instance.playerCardPoints)
            {
                if (point.activeCard != null)
                {
                    point.activeCard.MoveToPoint(discardPoint.position, point.activeCard.transform.rotation);
                }
            }
        }


        StartCoroutine(ShowResultCo());
        
    }

    IEnumerator ShowResultCo()
    {
        yield return new WaitForSeconds(resultScreenDelayTime);

        UIController.instance.battleEndScreen.SetActive(true);
    }
}
