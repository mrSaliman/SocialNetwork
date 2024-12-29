namespace SocialNetwork.Data;
using System.Data.SQLite;
using Models;

public class GroupRepository(string connectionString)
{
    public int CreateGroup(Group group)
    {
        using var connection = new SQLiteConnection(connectionString);
        connection.Open();

        using var command = new SQLiteCommand(connection);
        command.CommandText = "INSERT INTO Groups (Name) VALUES (@name)";
        command.Parameters.AddWithValue("@name", group.Name);

        try
        {
            command.ExecuteNonQuery();
            return (int)connection.LastInsertRowId;
        }
        catch (SQLiteException)
        {
            return -1;
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

    public List<Group> GetGroups(int userId)
    {
        var groups = new List<Group>();

        using var connection = new SQLiteConnection(connectionString);
        connection.Open();

        using var command = new SQLiteCommand(connection);
        command.CommandText = @"
        SELECT g.Id, g.Name
        FROM Groups g
        INNER JOIN GroupMembers gm ON g.Id = gm.GroupId
        WHERE gm.UserId = @userId";
        command.Parameters.AddWithValue("@userId", userId);

        try
        {
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                var group = new Group
                {
                    Id = reader.GetInt32(0),
                    Name = reader.GetString(1)
                };
                groups.Add(group);
            }
        }
        catch (SQLiteException ex)
        {
            Console.WriteLine($"Error retrieving groups: {ex.Message}");
        }

        return groups;
    }

    public Group? GetGroup(int groupId)
    {
        using var connection = new SQLiteConnection(connectionString);
        connection.Open();

        using var command = new SQLiteCommand(connection);
        command.CommandText = "SELECT Id, Name FROM Groups WHERE Id = @groupId";
        command.Parameters.AddWithValue("@groupId", groupId);

        try
        {
            using var reader = command.ExecuteReader();
            if (reader.Read())
            {
                return new Group
                {
                    Id = reader.GetInt32(0),
                    Name = reader.GetString(1)
                };
            }
        }
        catch (SQLiteException ex)
        {
            Console.WriteLine($"Error retrieving group: {ex.Message}");
        }

        return null;
    }
}
