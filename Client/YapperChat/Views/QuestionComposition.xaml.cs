using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;

namespace YapperChat.Views
{
    public partial class QuestionComposition : PhoneApplicationPage
    {
        public QuestionComposition()
        {
            InitializeComponent();
        }

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            string[] pollInfoStrings = new string[] { "Yes", "No", "Pass" };
            for (int i = 0; i < pollInfoStrings.Length; i++)
            {
                TextBlock option = new TextBlock();
                option.Text = Convert.ToString(i+1) + ".";
                option.Visibility = System.Windows.Visibility.Visible;
                this.QuestionCompositionStack.Children.Add(option);

                TextBox optionbox = new TextBox();
                optionbox.Name = "PollOptionEntry" + i;
                optionbox.Text = pollInfoStrings[i];
                this.QuestionCompositionStack.Children.Add(optionbox);
            }
        }

        private void CreatePollApplicationBarIconButton_Click(object sender, EventArgs e)
        {
            string newMessage = "???" + "#";
            foreach (UIElement c in this.QuestionCompositionStack.Children)
            {
                TextBox temp = c as TextBox;
                if (temp != null)
                {
                    if (temp.Name.StartsWith("PollOptionEntry"))
                    {
                        newMessage += temp.Text;
                    }
                }
            }

        }

        private void BuildLocalizedApplicationBar()
        {
            ApplicationBar appbar = (ApplicationBar)this.Resources["NewPollApplicationBar"];
            ((ApplicationBarIconButton)appbar.Buttons[0]).Text = YapperChat.Resources.Strings.NewPollText;
        }
    }
}