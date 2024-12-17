namespace SocialNetwork.Data;
using System.Data.SQLite;
using Models;

public class GroupRepository(string connectionString)
{
    public bool CreateGroup(Group group)
    {
        using var connection = new SQLiteConnection(connectionString);
        connection.Open();

        using var command = new SQLiteCommand(connection);
        command.CommandText = "INSERT INTO Groups (Name) VALUES (@name)";
        command.Parameters.AddWithValue("@name", group.Name);

        try
        {
            command.ExecuteNonQuery();
            return true;
        }
        catch (SQLiteException)
        {
            return false;
        }
    }

    public bool AddMember(GroupMember member)
    {
        using var connection = new SQLiteConnection(connectionString);
        connection.Open();

        using var command = new SQLiteCommand(connection);
        command.CommandText = "INSERT INTO GroupMembers (GroupId, UserId) VALUES (@groupId, @userId)";
        command.Parameters.AddWithValue("@groupId", member.GroupId);
        command.Parameters.AddWithValue("@userId", member.UserId);

        try
        {
            command.ExecuteNonQuery();
            return true;
        }
        catch (SQLiteException)
        {
            return false;
        }
    }
}