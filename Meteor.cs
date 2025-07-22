using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class Meteor : MonoBehaviour
{
    [Header("Movimento")]
    public float moveSpeed = 5f;

    [Header("Separação de Fragmentos")]
    public float separationSpeed = 2f;
    public float speedVariance   = 0.5f;

    [Header("Delay de Explosão")]
    public float explodeDelay    = 1f;

    [Header("Física dos Fragmentos")]
    public float fragmentMass    = 0.1f;

    private bool hasExploded     = false;
    private bool waitingToExplode = false;

    void Update()
    {
        if (!hasExploded && !waitingToExplode)
        {
            transform.position += Vector3.left * moveSpeed * Time.deltaTime;
            if (transform.position.x < -20f)
                Destroy(gameObject);
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        if (hasExploded || waitingToExplode) return;

        if (collision.collider.CompareTag("Player"))
        {
            waitingToExplode = true;
            StartCoroutine(DelayedExplode());
        }
    }

    private IEnumerator DelayedExplode()
    {
        // pausa o movimento
        yield return new WaitForSeconds(explodeDelay);

        ExplodeFragments();
        hasExploded = true;
        GameManager.Instance.GameOver();
    }

    private void ExplodeFragments()
    {
        Vector3 center = transform.position;
        var fragments  = new List<Transform>();

        // coleta todos os filhos
        foreach (Transform frag in transform)
            fragments.Add(frag);

        // desacopla
        transform.DetachChildren();

        foreach (var frag in fragments)
        {
            // certifica-se de que o Rigidbody existe
            Rigidbody rb = frag.gameObject.GetComponent<Rigidbody>();
            if (rb == null)
                rb = frag.gameObject.AddComponent<Rigidbody>();

            // agora é seguro configurar
            rb.mass       = fragmentMass;
            rb.useGravity = false;

            // direção radial
            Vector3 dir = (frag.position - center).normalized;
            if (dir == Vector3.zero)
                dir = Vector3.right;

            // velocidade de separação + variação
            float sepSpeed = separationSpeed + Random.Range(-speedVariance, speedVariance);

            // combinação de voo à esquerda e separação
            rb.linearVelocity = Vector3.left * moveSpeed + dir * sepSpeed;
        }

        Destroy(gameObject);
    }
}
