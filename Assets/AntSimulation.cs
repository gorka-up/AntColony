using UnityEngine;

public class AntSimulation : MonoBehaviour
{
    [Header("Simulation Options")]
    [RangeAttribute(1,1000)]
    [SerializeField] int antNumber = 300;
    [RangeAttribute(1, 10)]
    [SerializeField] float pheromonePower = 5f;
    [RangeAttribute(0, 1)]
    [SerializeField] float evaporationRate = 0.5f;
    [RangeAttribute(0, 1)]
    [SerializeField] float exploration = 0.5f;


    [Header("Field Options")]
    [SerializeField] int width = 120;
    [SerializeField] int height = 80;

    [Header("Ant Options")]
    [SerializeField] float speed = 15f;
    [SerializeField] float sensorLenght = 5f;
    [SerializeField] float sensorAngle = 0.6f;

    struct Ant
    {
        public Vector2 pos;
        public float angle;
        public bool hasFood;
    }
    private Ant[] Ants;

    //Tableros
    private float[,] pheromones;
    private bool[,] food;

    //Spawn de hormigas
    [SerializeField] private Vector2 nest;
    [SerializeField] private float nestRadius = 3f;

    //Representacion Visual 
    Texture2D texture;
    Color32[] pixels;

    [SerializeField] int brushRadius = 2;

    void Start()
    {
        Reset();
    }


    void Update()
    {
        Step();
        ShowMap();
    }

    private void Reset()
    {
        pheromones = new float[width, height];
        food = new bool[width, height];

        Ants = new Ant[antNumber];
        for (int i = 0; i < antNumber; i++)
        {
            Ants[i] = new Ant { pos = nest, angle = Random.value * Mathf.PI * 2f };
        }

        texture = new Texture2D(width, height) { filterMode = FilterMode.Point };
        pixels = new Color32[width * height];
    }

    void Step()
    {
        for (int i = 0; i < Ants.Length; i++)
        {
            ref Ant ant = ref Ants[i];

            //Si comida -> vuelve a casa
            if (ant.hasFood)
            {
                Vector2 dirHome = nest - ant.pos;
                ant.angle = Mathf.Atan2(dirHome.y, dirHome.x);
            }
            //Si no -> lee feromonas alrededor
            else
            {
                float left = ReadPheromones(ant, sensorAngle);
                float center = ReadPheromones(ant, 0f);
                float right = ReadPheromones(ant, -sensorAngle);

                if (left > center && left > right) ant.angle += 0.2f;
                else if (right > center && right > left) ant.angle -= 0.2f;
            }
            //Variacion aleatoria
            ant.angle += Random.Range(-1f, 1f) * exploration * 0.3f;

            //Nueva direccion
            Vector2 dir = new Vector2(Mathf.Cos(ant.angle), Mathf.Sin(ant.angle));
            Vector2 newPos = ant.pos + dir * speed * Time.deltaTime;

            if (newPos.x < 0 || newPos.y < 0 || newPos.x >= width || newPos.y >= height)
            {
                ant.angle += Mathf.PI;
            }
            else
            {
                ant.pos = newPos;
            }

            int posX = (int)ant.pos.x;
            int posY = (int)ant.pos.y;

            //Si hay comida la recoge
            if (!ant.hasFood && food[posX, posY])
            {
                food[posX, posY] = false;
                ant.hasFood = true;
            }
            //Si tiene comida deja feromonas
            else if (ant.hasFood)
            {
                pheromones[posX, posY] = Mathf.Min(pheromones[posX, posY] + 1f, pheromonePower);

                //Deja la comida y sale
                if (Vector2.Distance(ant.pos, nest) < nestRadius)
                {
                    ant.hasFood = false;
                    ant.angle += Mathf.PI;
                }
            }
        }

        //La feromona se evapora poco a poco
        float evaporationFactor = 1f - evaporationRate * Time.deltaTime;
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                pheromones[x, y] *= evaporationFactor;
            }
        }
            
    }

    float ReadPheromones(Ant ant, float offset)
    {
        float angle = ant.angle + offset;
        Vector2 pos = ant.pos + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * sensorLenght;

        if (pos.x < 0 || pos.y < 0 || pos.x >= width || pos.y >= height) return -1f;
        return pheromones[(int)pos.x, (int)pos.y];
    }

    void ShowMap()
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                int pixel = y * width + x;

                if (food[x, y])
                {
                    pixels[pixel] = Color.green;
                }
                else
                {
                    pixels[pixel] = Color.Lerp(Color.black, Color.red, pheromones[x, y] / 2f);
                }

                if (Vector2.Distance(new Vector2(x, y), nest) < nestRadius)
                {
                    pixels[pixel] = Color.orange;
                }
            }
        }

        Color32 color = new Color32();
        foreach (var ant in Ants)
        {
            int pixel = (int)ant.pos.y * width + (int)ant.pos.x;

            if (ant.hasFood)
            {
                color = Color.yellow;
            }
            else
            {
                color = Color.white;
            }
            pixels[pixel] = color;
        }

        texture.SetPixels32(pixels);
        texture.Apply();
    }

    void Draw()
    {
        Event e = Event.current;

        if (e.type != EventType.MouseDown && e.type != EventType.MouseDrag) return;

        float mouseX = e.mousePosition.x / Screen.width * width;
        float mouseY = (1f - e.mousePosition.y / Screen.height) * height;
        Vector2 mouse = new Vector2(mouseX, mouseY);

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (Vector2.Distance(new Vector2(x, y), mouse) > brushRadius) continue;

                if (e.button == 0) pheromones[x, y] = pheromonePower;
                if (e.button == 1) food[x, y] = true;
            }
        }
            
        e.Use();
    }

    void OnGUI()
    {
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), texture);
        Draw();
    }
}
