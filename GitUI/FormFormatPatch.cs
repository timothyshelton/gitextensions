﻿using System;
using System.IO;
using System.Net.Mail;
using System.Windows.Forms;
using GitCommands;
using ResourceManager.Translation;

namespace GitUI
{
    public partial class FormFormatPatch : GitExtensionsForm
    {
        private readonly TranslationString _currentBranchText = new TranslationString("Current branch:");
        private readonly TranslationString _noOutputPathEnteredText = 
            new TranslationString("You need to enter an output path.");
        private readonly TranslationString _noEmailEnteredText = 
            new TranslationString("You need to enter an email address.");
        private readonly TranslationString _noSubjectEnteredText = 
            new TranslationString("You need to enter a mail subject.");
        private readonly TranslationString _wrongSmtpSettingsText = 
            new TranslationString("You need to enter a valid smtp in the settings dialog.");
        private readonly TranslationString _twoRevisionsNeededText =
            new TranslationString("You need to select two revisions");
        private readonly TranslationString _twoRevisionsNeededCaption =
            new TranslationString("Patch error");
        private readonly TranslationString _sendMailResult =
            new TranslationString("\n\nSend to:");
        private readonly TranslationString _sendMailResultFailed =
            new TranslationString("\n\nFailed to send mail.");
        private readonly TranslationString _patchResultCaption =
            new TranslationString("Patch result");
        private readonly TranslationString _noGitMailConfigured =
            new TranslationString("There is no email address configured in the settings dialog.");

        public FormFormatPatch()
        {
            InitializeComponent(); Translate();
        }

        private void Browse_Click(object sender, EventArgs e)
        {
            var dialog = new FolderBrowserDialog();
            if (dialog.ShowDialog(this) == DialogResult.OK)
                OutputPath.Text = dialog.SelectedPath;
        }

        private void FormFormatPath_Load(object sender, EventArgs e)
        {
            OutputPath.Text = Settings.LastFormatPatchDir;
            string selectedHead = Settings.Module.GetSelectedBranch();
            SelectedBranch.Text = _currentBranchText.Text + " " + selectedHead;

            SaveToDir_CheckedChanged(null, null);
            OutputPath.TextChanged += OutputPath_TextChanged;
            RevisionGrid.Load();
        }

        private void OutputPath_TextChanged(object sender, EventArgs e)
        {
            if (Directory.Exists(OutputPath.Text))
               Settings.LastFormatPatchDir = OutputPath.Text;
        }

        private void FormatPatch_Click(object sender, EventArgs e)
        {
            if (SaveToDir.Checked && string.IsNullOrEmpty(OutputPath.Text))
            {
                MessageBox.Show(this, _noOutputPathEnteredText.Text);
                return;
            }

            if (!SaveToDir.Checked && string.IsNullOrEmpty(MailAddress.Text))
            {
                MessageBox.Show(this, _noEmailEnteredText.Text);
                return;
            }

            if (!SaveToDir.Checked && string.IsNullOrEmpty(MailSubject.Text))
            {
                MessageBox.Show(this, _noSubjectEnteredText.Text);
                return;
            }

            if (!SaveToDir.Checked && string.IsNullOrEmpty(Settings.Smtp))
            {
                MessageBox.Show(this, _wrongSmtpSettingsText.Text);
                return;
            }

            string savePatchesToDir = OutputPath.Text;

            if (!SaveToDir.Checked)
            {
                savePatchesToDir = Settings.Module.WorkingDirGitDir() + "\\PatchesToMail";
                if (Directory.Exists(savePatchesToDir))
                {
                    foreach (string file in Directory.GetFiles(savePatchesToDir, "*.patch"))
                        File.Delete(file);
                }
                else
                {
                    Directory.CreateDirectory(savePatchesToDir);
                }
            }

            string rev1 = "";
            string rev2 = "";
            string result = "";

            if (RevisionGrid.GetRevisions().Count > 0)
            {
                if (RevisionGrid.GetRevisions().Count == 1)
                {
                    rev1 = RevisionGrid.GetRevisions()[0].ParentGuids[0];
                    rev2 = RevisionGrid.GetRevisions()[0].Guid;
                    result = Settings.Module.FormatPatch(rev1, rev2, savePatchesToDir);
                }

                if (RevisionGrid.GetRevisions().Count == 2)
                {
                    rev1 = RevisionGrid.GetRevisions()[0].ParentGuids[0];
                    rev2 = RevisionGrid.GetRevisions()[1].Guid;
                    result = Settings.Module.FormatPatch(rev1, rev2, savePatchesToDir);
                }

                if (RevisionGrid.GetRevisions().Count > 2)
                {
                    int n = 0;
                    foreach (GitRevision revision in RevisionGrid.GetRevisions())
                    {
                        n++;
                        rev1 = revision.ParentGuids[0];
                        rev2 = revision.Guid;
                        result += Settings.Module.FormatPatch(rev1, rev2, savePatchesToDir, n);
                    }
                }
            }
            else
                if (string.IsNullOrEmpty(rev1) || string.IsNullOrEmpty(rev2))
                {
                    MessageBox.Show(this, _twoRevisionsNeededText.Text, _twoRevisionsNeededCaption.Text);
                    return;
                }

            if (!SaveToDir.Checked)
            {
                if (SendMail(savePatchesToDir))
                    result += _sendMailResult.Text + " " + MailAddress.Text;
                else
                    result += _sendMailResultFailed.Text;


                //Clean up
                if (Directory.Exists(savePatchesToDir))
                {
                    foreach (string file in Directory.GetFiles(savePatchesToDir, "*.patch"))
                        File.Delete(file);
                }
            }

            MessageBox.Show(this, result, _patchResultCaption.Text);
            Close();
        }

        private bool SendMail(string dir)
        {
            try
            {
                string from = Settings.Module.GetSetting("user.email");

                if (string.IsNullOrEmpty(from))
                    from = Settings.Module.GetGlobalSetting("user.email");

                if (string.IsNullOrEmpty(from))
                    MessageBox.Show(this, _noGitMailConfigured.Text);

                string to = MailAddress.Text;

                using (var mail = new MailMessage(from, to, MailSubject.Text, MailBody.Text))
                {
                    foreach (string file in Directory.GetFiles(dir, "*.patch"))
                    {
                        var attacheMent = new Attachment(file);
                        mail.Attachments.Add(attacheMent);
                    }

                    var smtpClient = new SmtpClient(Settings.Smtp);
                    smtpClient.Send(mail);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message);
                return false;
            }
            return true;
        }

        private void SaveToDir_CheckedChanged(object sender, EventArgs e)
        {
            OutputPath.Enabled = SaveToDir.Checked;
            MailAddress.Enabled = !SaveToDir.Checked;
            MailSubject.Enabled = !SaveToDir.Checked;
            MailBody.Enabled = !SaveToDir.Checked;
        }
    }
}
