using UnityEngine;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

public static class SaveSystem
{
    public static void SaveGame()
    {
        BinaryFormatter formatter = new BinaryFormatter();
        string path = Application.persistentDataPath + "/player.save";
        FileStream stream = new FileStream(path, FileMode.Create);

        PlayerData data = new PlayerData();
        data.position = GameObject.FindGameObjectWithTag("Player").transform.position;
        data.health = GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerHealth>().maxHealth; // упрощённо
        // Добавьте другие данные: режим оружия, прогресс головоломок и т.д.

        formatter.Serialize(stream, data);
        stream.Close();
    }

    public static void LoadGame()
    {
        string path = Application.persistentDataPath + "/player.save";
        if (File.Exists(path))
        {
            BinaryFormatter formatter = new BinaryFormatter();
            FileStream stream = new FileStream(path, FileMode.Open);
            PlayerData data = formatter.Deserialize(stream) as PlayerData;
            stream.Close();

            Transform player = GameObject.FindGameObjectWithTag("Player").transform;
            player.position = data.position;
            player.GetComponent<PlayerHealth>().maxHealth = data.health; // упрощённо
            // восстановить остальное
        }
    }
}

[System.Serializable]
public class PlayerData
{
    public Vector3 position;
    public int health;
}