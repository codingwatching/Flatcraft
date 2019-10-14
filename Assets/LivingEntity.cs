﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LivingEntity : Entity
{
    //Entity Properties
    public static Color damageColor = new Color(1, 0.5f, 0.5f, 1);
    public virtual float maxHealth { get; } = 20;
    
    [Header("Movement Properties")]
    private float walkSpeed = 4.3f;
    private float sprintSpeed = 5.6f;
    private float sneakSpeed = 1.3f;
    private float jumpVelocity = 10f;
    private float swimVelocity = 6f;
    private float groundFriction = 0.7f;
    private float airDrag = 0.98f;
    private float liquidDrag = 0.5f;

    //Entity Data Tags
    [EntityDataTag(false)]
    public float health;
    

    //Entity State
    private float last_jump_time;
    private float highestYlevelsinceground;
    public EntityController controller;

    public override void Start()
    {
        base.Start();

        controller = GetController();
        health = maxHealth;

        GetComponent<Rigidbody2D>().constraints = RigidbodyConstraints2D.FreezeRotation;
    }

    public virtual void FixedUpdate()
    {
        ProcessMovement();
        fallDamageCheck();
    }

    public override void Update()
    {
        base.Update();

        controller.Tick();
        CalculateFlip();
        UpdateAnimatorValues();
    }

    public virtual void UpdateAnimatorValues()
    {
        Animator anim = GetComponent<Animator>();

        if (anim == null)
            return;

        anim.SetFloat("velocity", getVelocity().x);
    }

    public virtual void ProcessMovement()
    {
        ApplyFriction();
    }

    public virtual void ApplyFriction()
    {
        if (isInLiquid)
            setVelocity(getVelocity() * liquidDrag);
        if (!isInLiquid && !isOnGround)
            setVelocity(new Vector3(getVelocity().x * airDrag, getVelocity().y));
        if (!isInLiquid && isOnGround)
            setVelocity(getVelocity() * groundFriction);
    }
    
    public virtual void Walk(int direction)
    {
        if (getVelocity().x < walkSpeed && getVelocity().x > -walkSpeed)
        {
            float targetXVelocity = 0;

            if (direction == -1)
                targetXVelocity -= walkSpeed;
            else if (direction == 1)
                targetXVelocity += walkSpeed;
            else targetXVelocity = 0;
            
            setVelocity(new Vector2(targetXVelocity, getVelocity().y));
        }
    }

    public virtual EntityController GetController()
    {
        return new EntityController(this);
    }

    public virtual void CalculateFlip()
    {
        if (getVelocity().x != 0)
        {
            flipRenderX = (getVelocity().x < 0);
        }
    }

    public virtual void Jump()
    {
        if (isOnGround)
        {
            if (Time.time - last_jump_time < 0.7f)
                return;

            setVelocity(getVelocity() + new Vector2(0, jumpVelocity));
            last_jump_time = Time.time;
        }
        if (isInLiquid && getVelocity().y < swimVelocity)
        {
            setVelocity(getVelocity() + new Vector2(0, swimVelocity*0.1f));
        }
    }


    private void fallDamageCheck()
    {
        if (isOnGround && !isInLiquid)
        {
            float damage = (highestYlevelsinceground - transform.position.y) - 3;
            if (damage >= 1)
                TakeFallDamage(damage);
        }

        if (isOnGround || isInLiquid)
            highestYlevelsinceground = 0;
        else if (transform.position.y > highestYlevelsinceground)
            highestYlevelsinceground = transform.position.y;
    }
    
    public virtual void TakeFallDamage(float damage)
    {
        Damage(damage);
    }

    public override void Damage(float damage)
    {
        health -= damage;

        if (health <= 0)
            Die();

        StartCoroutine(TurnRedByDamage());
    }

    public override void Die()
    {
        DeathSmoke();

        base.Die();
    }

    public virtual void DeathSmoke()
    {
        System.Random rand = new System.Random();
        for (int x = 0; x < 4; x++)
        {
            for (int y = 0; y < 4; y++)
            {
                if (rand.NextDouble() < 0.2f)
                {
                    Particle part = (Particle)Entity.Spawn("Particle");

                    part.transform.position = (transform.position - new Vector3(0.5f, 0.5f)) + new Vector3(0.25f * x, 0.25f * y);
                    part.color = Color.white;
                    part.doGravity = false;
                    part.velocity = new Vector2(0, 0.3f + (float)rand.NextDouble() * 0.5f);
                    part.maxAge = 0.5f + (float)rand.NextDouble();
                }
            }
        }
    }

    IEnumerator TurnRedByDamage()
    {
        Color baseColor = getRenderer().color;

        getRenderer().color = damageColor;
        yield return new WaitForSeconds(0.15f);
        getRenderer().color = baseColor;
    }
}
