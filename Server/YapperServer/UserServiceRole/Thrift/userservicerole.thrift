namespace csharp UserServiceRole

enum UserType{
  User = 0,
  Group = 1,
}

struct UserData {
  1: i32 Id = 0,
  2: string PhoneNumber,
  3: string Name,
  4: string Secret,
  5: UserType UserType,
  6: i64 LastSyncTimeTicks,
  7: optional binary PublicKey,
}

struct GroupData {
  1: UserData Owner,
  2: list<UserData> Members,
}

struct User {
  1: UserData UserData,
  2: optional GroupData GroupData,
}

struct UserCookie
{
   1: i32 UserId,
   2: string DeviceId,
   3: string Cookie,
}

/**
 * Structs can also be exceptions, if they are nasty.
 */
exception InvalidOperation {
  1: i32 what,
  2: string why
}

service UserService {

        UserCookie ValidateUser(1:string phoneNumber, 2:i32 oneTimePassword, 3:string deviceId, 4:string random),

        User RegisterUser(1:string phoneNumber, 2:string name, 3:string deviceId),

        User CreateGroup(1:User newGroup),

        bool AddUserToGroup(1:i32 groupId, 2:string user),

        bool RemoveUserFromGroup(1:i32 groupId, 2:string user),

        list<User> GetGroups(),

        list<User> GetUsers(1:list<string> phoneNumbers),
}
