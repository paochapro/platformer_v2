using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended;

namespace Lib;
using static Utils;

class ParticleEmitter : Entity
{
    private static float minLifeTime = 0.2f;
    private static float minSpeed = 2f;
    private List<Particle> particles = new();
    private List<Particle> removed = new();

    public ParticleEmitter(Point2 position, int count, float maxSpeed, float maxLifeTime)
    {
        Random rand = new Random((int)DateTime.Now.Ticks);
        
        for (int i = 0; i < count; ++i)
        {
            Vector2 direction = new Vector2(rand.NextSingle(-1,1), rand.NextSingle(-1,1)).NormalizedCopy();
            float speed = rand.NextSingle(minSpeed, maxSpeed);
            float lifeTime = rand.NextSingle(minLifeTime, maxLifeTime);

            Particle particle = new(this, position, direction, speed, lifeTime);
            particles.Add(particle);
        }
        
        Event.Add(Destroy, maxLifeTime);
    }

    protected override void Update(GameTime gameTime)
    {
        particles.ForEach(p => p.Update(gameTime));
        removed.ForEach(p => particles.Remove(p));
        removed.Clear();
    }
    
    protected override void Draw(SpriteBatch spriteBatch)
    {
        particles.ForEach(p => p.Draw(spriteBatch));
    }

    private class Particle
    {
        private ParticleEmitter particleEmitter;
        private Point2 position;
        private Vector2 direction;
        private float speed;
        private float lifeTime;
        private float livingTime;

        public Particle(ParticleEmitter particleEmitter, Point2 position, Vector2 direction, float speed, float lifeTime)
        {
            this.particleEmitter = particleEmitter;
            this.position = position;
            this.direction = direction;
            this.speed = speed;
            this.lifeTime = lifeTime;
        }

        public void Update(GameTime gameTime)
        {
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
            position += direction * speed * dt;

            livingTime += dt;
            if (livingTime > lifeTime)
            {
                particleEmitter.removed.Add(this);
            }
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            spriteBatch.DrawPoint(position, Color.White);
        }
    }
}