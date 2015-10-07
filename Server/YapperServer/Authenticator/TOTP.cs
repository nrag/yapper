using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Authenticator
{
    /**
    * TOTP - One time password generator 
    * 
    * The TOTP class allow for the generation 
    * and verification of one-time password using 
    * the TOTP specified algorithm.
    *
    * This class is meant to be compatible with 
    * Google Authenticator
    *
    * This class was originally ported from the rotp
    * ruby library available at https://github.com/mdp/rotp
    */
    public class TOTP : OTP
    {
        /// <summary>
        /// Password is valid for 5 mins
        /// </summary>
        private static int PasswordValidIntervals = 20;

        /// <summary>
        /// The time timeframe when the one-time password will not change
        /// It's set to 30 secs by default
        /// </summary>
        private double interval;

        public TOTP(string secret)
            : this(secret, 30)
        {
        }

        public TOTP(string secret, double interval)
            : this(secret, interval, 6)
        {
        }

        public TOTP(string secret, double interval, int digits)
            : base(secret, digits)
        {
            this.interval = interval;
        }

        /**
         *  Get the password for a specific timestamp value 
         *
         *  @param integer $timestamp the timestamp which is timecoded and 
         *  used to seed the hmac hash function.
         *  @return integer the One Time Password
         */
        public int At(long timestamp)
        {
            return this.GenerateOTP(this.NumberOfIntervals(timestamp));
        }

        /**
         *  Get the password for the current timestamp value 
         *
         *  @return integer the current One Time Password
         */
        public int Now()
        {
            return this.At(new Unixtime().ToTimeStamp());
        }

        public bool Verify(int otp)
        {
            long timeStamp = new Unixtime().ToTimeStamp();
            long numberOfIntervals = this.NumberOfIntervals(timeStamp);

            for (int i = 0; i < PasswordValidIntervals; i++)
            {
                if (this.GenerateOTP(numberOfIntervals - i) == otp)
                {
                    return true;
                }
            }

            return false;
        }

        /**
         * Transform a timestamp in a counter based on specified internal
         *
         * @param integer $timestamp
         * @return integer the timecode
         */
        public Int64 NumberOfIntervals(long seconds)
        {
            return (Int64)(((((seconds * 1000)) / (this.interval * 1000))));
        }
    }
}
