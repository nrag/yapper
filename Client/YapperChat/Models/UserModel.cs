using System;
using System.ComponentModel;
using System.Data.Linq;
using System.Data.Linq.Mapping;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Runtime.Serialization;
using System.Windows.Media.Imaging;
using Microsoft.Phone.UserData;
using System.IO.IsolatedStorage;
using System.IO;
using Microsoft.Phone;

namespace YapperChat.Models
{
    public enum UserType
    {
        User = 0,

        Group = 1
    }

    [KnownType(typeof(GroupModel))]
    [DataContract(Namespace="http://schemas.datacontract.org/2004/07/Yapper")]
    [Table]
    [InheritanceMapping(Code = "Group", Type = typeof(GroupModel), IsDefault = false)]
    [InheritanceMapping(Code = "User", Type = typeof(UserModel), IsDefault = true)]
    public class UserModel : IEquatable<UserModel>, INotifyPropertyChanged, IComparable
    {
        private UserType _userType = UserType.User;

        /// <summary>
        /// Image to be displayed for the other participant
        /// </summary>
        private BitmapImage contactPhoto = null;

        [DataMember]
        [Column(IsPrimaryKey = true, IsDbGenerated = false)]
        public int Id
        {
            get;
            set;
        }

        [DataMember]
        [Column(CanBeNull=true)]
        public string PhoneNumber
        {
            get;
            set;
        }

        [DataMember]
        [Column]
        public string Name
        {
            get;
            set;
        }

        [DataMember]
        [Column]
        public UserType UserType
        {
            get
            {
                return this._userType;
            }

            set
            {
                this._userType = value;
            }
        }

        [DataMember]
        [Column(CanBeNull=true)]
        public byte[] PublicKey
        {
            get;
            set;
        }

        [Column(IsDiscriminator = true)]
        public string Discriminator
        {
            get;
            set;
        }

        public bool IsGroupOwner
        {
            get
            {
                if (this.UserType == Models.UserType.User)
                {
                    return false;
                }

                if (((GroupModel)this).OwnerId != UserSettingsModel.Instance.Me.Id)
                {
                    return false;
                }

                return true;
            }
        }

        public bool IsGroupMember
        {
            get
            {
                if (this.UserType == Models.UserType.User)
                {
                    return false;
                }

                if (((GroupModel)this).Members != null && !((GroupModel)this).Members.Contains(UserSettingsModel.Instance.Me))
                {
                    return false;
                }

                return true;
            }
        }

        public BitmapImage ContactPhoto
        {
            get
            {
                if (this.contactPhoto == null && this.UserType == UserType.Group)
                {
                    this.contactPhoto = new BitmapImage(new Uri("/Images/default.group.profile.png", UriKind.RelativeOrAbsolute));
                }
                else if (this.contactPhoto == null && string.IsNullOrEmpty(this.PhoneNumber))
                {
                    this.contactPhoto = new BitmapImage(new Uri("/Images/default.profile.png", UriKind.RelativeOrAbsolute));
                }
                else if (this.contactPhoto == null)
                {
                    try
                    {
                        this.LoadContactPhoto();
                    }
                    catch (Exception)
                    {
                        this.contactPhoto = new BitmapImage(new Uri("/Images/default.profile.png", UriKind.RelativeOrAbsolute));
                    }

                    if (this.contactPhoto == null)
                    {
                        this.contactPhoto = new BitmapImage(new Uri("/Images/default.profile.png", UriKind.RelativeOrAbsolute));
                    }
                }

                return this.contactPhoto;
            }
        }

        public override bool Equals(object other)
        {
            UserModel otherGroup = other as UserModel;

            if (otherGroup == null)
            {
                return false;
            }

            return this.Equals(otherGroup);
        }

        public bool Equals(UserModel other)
        {
            return this.Id == other.Id;
        }

        public override int GetHashCode()
        {
            return this.Id;
        }

        private void LoadContactPhoto()
        {
            if (this.UserType == UserType.Group)
            {
                this.contactPhoto = new BitmapImage(new Uri("/Images/group.png", UriKind.RelativeOrAbsolute));
                return;
            }

            if (string.IsNullOrEmpty(this.PhoneNumber))
            {
                this.contactPhoto = new BitmapImage(new Uri("/Images/default.profile.png", UriKind.RelativeOrAbsolute));
                return;
            }

            var isoFile = IsolatedStorageFile.GetUserStoreForApplication();

            if (isoFile.FileExists(string.Format("UserPhoto/{0}", this.Id.ToString())))
            {
                this.contactPhoto = this.GetImageFromIsolatedStorage();
                return;
            }

            if (isoFile.FileExists(string.Format("UserPhoto/{0}Default", this.Id.ToString())))
            {
                this.contactPhoto = new BitmapImage(new Uri("/Images/default.profile.png", UriKind.RelativeOrAbsolute));
                return;
            }

            var contactSearchArguments = new ContactSearchArguments(this.PhoneNumber, SearchKind.Picture, FilterKind.PhoneNumber, null);

            contactSearchArguments.SearchCompleted += (s, args) =>
            {
                Deployment.Current.Dispatcher.BeginInvoke(() =>
                {
                    if (args.Results != null)
                    {
                        this.contactPhoto = (BitmapImage)args.Results;
                        this.SavePhoto(this.contactPhoto);
                    }

                    if (this.contactPhoto == null)
                    {
                        this.contactPhoto = new BitmapImage(new Uri("/Images/default.profile.png", UriKind.RelativeOrAbsolute));
                        this.contactPhoto.CreateOptions = BitmapCreateOptions.None;
                        isoFile.CreateFile(string.Format("UserPhoto/{0}Default", this.Id.ToString()));
                    }

                    this.NotifyPropertyChanged("ContactPhoto");
                });
            };

            ContactSearchController.Instance.StartSearch(contactSearchArguments);
        }

        private void SavePhoto(BitmapImage image)
        {
            using (var isoFile = IsolatedStorageFile.GetUserStoreForApplication())
            {
                if (!isoFile.DirectoryExists("UserPhoto"))
                {
                    isoFile.CreateDirectory("UserPhoto");
                }

                byte[] imagebytes;
                using (MemoryStream ms = new MemoryStream())
                {
                    WriteableBitmap btmMap = new WriteableBitmap(image);

                    // write an image into the stream
                    Extensions.SaveJpeg(btmMap, ms, image.PixelWidth, image.PixelHeight, 0, 100);

                    imagebytes = ms.ToArray();
                }

                using (var stream = isoFile.CreateFile(string.Format("UserPhoto/{0}", this.Id.ToString())))
                {
                    stream.Write(imagebytes, 0, imagebytes.Length);
                }
            }
        }

        private BitmapImage GetImageFromIsolatedStorage()
        {
            FileStream imageStream = null;
            try
            {
                var isoFile = IsolatedStorageFile.GetUserStoreForApplication();
                using (imageStream = isoFile.OpenFile(string.Format("UserPhoto/{0}", this.Id.ToString()), FileMode.Open, FileAccess.Read))
                {
                    var imageSource = PictureDecoder.DecodeJpeg(imageStream);
                    return this.ConvertWriteableBitmapToBitmapImage(imageSource);
                }
            }
            catch (Exception)
            {
                // if the image is corrupt
                return null;
            }
            finally
            {
                if (imageStream != null)
                {
                    imageStream.Dispose();
                }
            }
        }

        private BitmapImage ConvertWriteableBitmapToBitmapImage(WriteableBitmap wb)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                wb.SaveJpeg(ms, wb.PixelHeight, wb.PixelWidth, 0, 100);
                BitmapImage bmp = new BitmapImage();
                bmp.SetSource(ms);

                return bmp;
            }
        }

        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;

        // Used to notify that a property changed
        private void NotifyPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        #endregion

        // Comparer that sorts User objects by Name
        public int CompareTo(object obj)
        {
            if (!(obj is UserModel))
            {
                // this precedes obj
                return -1;
            }

            UserModel other = obj as UserModel;

            return string.Compare(this.Name, other.Name, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Table]
    public class ConversationUserModel
    {
        [Column(IsPrimaryKey = true, IsDbGenerated = true)]
        public int UniqueColumnId
        {
            get;
            set;
        }

        [Column]
        public long ConversationId
        {
            get;
            set;
        }

        [Column]
        public int UserId
        {
            get;
            set;
        }
    }
}