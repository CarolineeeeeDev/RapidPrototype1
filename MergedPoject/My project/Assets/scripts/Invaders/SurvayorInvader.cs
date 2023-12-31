using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.CullingGroup;

public class SurvayerInvader : MonoBehaviour {

    [SerializeField] private GameObject Drop;
    [SerializeField] private Transform DropParent;

    [SerializeField] private int pointsAwarded = 1000;

    private void OnTriggerEnter2D(Collider2D collision) {
        if (collision.gameObject.layer == LayerMask.NameToLayer("Ball") ) {

            Instantiate(Drop, this.transform.position, Quaternion.identity, DropParent);

            ScoreManager.Instance.IncrementInvadersDestroyed();
            ScoreManager.Instance.IncrementSpecialInvadersDestroyed();
            ScoreManager.Instance.AddToScore(pointsAwarded);

            this.gameObject.SetActive(false);
        }
    }
}
