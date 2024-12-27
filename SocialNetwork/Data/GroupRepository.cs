namespace SocialNetwork.Data;
using System.Data.SQLite;
using Models;

public class GroupRepository(string connectionString)
{
    public bool CreateGroup(Group group)
    {
        using var connection = new SQLiteConnection(connectionString);
        connection.Open();

        if (GetGroupIdByName(group.Name) != null)
        {
            return false;
        }

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

        
        using var checkCommand = new SQLiteCommand(connection);
        checkCommand.CommandText = "SELECT COUNT(1) FROM GroupMembers WHERE GroupId = @groupId AND UserId = @userId";
        checkCommand.Parameters.AddWithValue("@groupId", member.GroupId);
        checkCommand.Parameters.AddWithValue("@userId", member.UserId);

        var exists = Convert.ToInt32(checkCommand.ExecuteScalar()) > 0;
        if (exists)
        {
            return false;
        }

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

    public int? GetGroupIdByName(string groupName)
    {
        using var connection = new SQLiteConnection(connectionString);
        connection.Open();

        using var command = new SQLiteCommand(connection);
        command.CommandText = "SELECT Id FROM Groups WHERE Name = @name";
        command.Parameters.AddWithValue("@name", groupName);

        using var reader = command.ExecuteReader();
        if (reader.Read())
        {
            return reader.GetInt32(0);
        }
        return null;
    }
}
