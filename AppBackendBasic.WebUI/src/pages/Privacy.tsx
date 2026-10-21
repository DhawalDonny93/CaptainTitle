import Header from '../components/Header';
import Footer from '../components/Footer';

export default function Privacy() {

  return (
    <div className="min-h-screen bg-white">
      <Header />

      {/* Main Content */}
      <main className="max-w-4xl mx-auto px-6 py-16">
        {/* Title */}
        <div className="mb-16">
          <h1 className="text-5xl font-light tracking-tight text-gray-900 mb-4">
            Privacy Policy
          </h1>
          <p className="text-gray-500 font-light">
            Last updated: {new Date().toLocaleDateString('en-US', { month: 'long', day: 'numeric', year: 'numeric' })}
          </p>
        </div>

        {/* Content */}
        <div className="prose prose-lg max-w-none">
          {/* Introduction */}
          <section className="mb-12">
            <p className="text-gray-700 font-light leading-relaxed mb-6">
              This Privacy Policy describes how BasicFramework ("we", "us", or "our") collects, uses, and shares your personal information when you use our application and services. We are committed to protecting your privacy and being transparent about our data practices.
            </p>
          </section>

          {/* Interpretation and Definitions */}
          <section className="mb-12">
            <h2 className="text-3xl font-light text-gray-900 mb-6">
              Interpretation and Definitions
            </h2>
            
            <h3 className="text-xl font-normal text-gray-900 mb-4">Interpretation</h3>
            <p className="text-gray-700 font-light leading-relaxed mb-6">
              The words of which the initial letter is capitalized have meanings defined under the following conditions. The following definitions shall have the same meaning regardless of whether they appear in singular or in plural.
            </p>

            <h3 className="text-xl font-normal text-gray-900 mb-4">Definitions</h3>
            <p className="text-gray-700 font-light leading-relaxed mb-4">
              For the purposes of this Privacy Policy:
            </p>
            <ul className="space-y-3 mb-6">
              <li className="text-gray-700 font-light">
                <strong className="font-medium text-gray-900">Account</strong> means the unique profile created for You to access the Service.
              </li>
              <li className="text-gray-700 font-light">
                <strong className="font-medium text-gray-900">Application</strong> refers to BasicFramework, the software program provided by the Company.
              </li>
              <li className="text-gray-700 font-light">
                <strong className="font-medium text-gray-900">Company</strong> (referred to as either "the Company", "We", "Us" or "Our" in this Agreement) refers to BasicFramework.
              </li>
              <li className="text-gray-700 font-light">
                <strong className="font-medium text-gray-900">Device</strong> means any device that can access the Service such as a computer, a cellphone or a digital tablet.
              </li>
              <li className="text-gray-700 font-light">
                <strong className="font-medium text-gray-900">Personal Data</strong> means information that identifies or can reasonably be linked to an individual.
              </li>
              <li className="text-gray-700 font-light">
                <strong className="font-medium text-gray-900">Usage Data</strong> means data collected automatically through use of the Service, including device information, activity logs, and analytics data.
              </li>
              <li className="text-gray-700 font-light">
                <strong className="font-medium text-gray-900">User Content</strong> means photos, images, text, or other inputs You upload or submit through the Service.
              </li>
              <li className="text-gray-700 font-light">
                <strong className="font-medium text-gray-900">Service</strong> refers to the Application and Website.
              </li>
              <li className="text-gray-700 font-light">
                <strong className="font-medium text-gray-900">Website</strong> means your deployed application and any associated domains.
              </li>
              <li className="text-gray-700 font-light">
                <strong className="font-medium text-gray-900">You</strong> means the individual accessing or using the Service, or the company, or other legal entity on behalf of which such individual is accessing or using the Service, as applicable.
              </li>
            </ul>
          </section>

          {/* Collecting and Using Your Personal Data */}
          <section className="mb-12">
            <h2 className="text-3xl font-light text-gray-900 mb-6">
              Collecting and Using Your Personal Data
            </h2>

            <h3 className="text-xl font-normal text-gray-900 mb-4">Types of Data Collected</h3>
            
            <h4 className="text-lg font-medium text-gray-900 mb-3">Personal Data You Provide</h4>
            <p className="text-gray-700 font-light leading-relaxed mb-4">
              We may collect the following Personal Data when You create an Account or use the Service:
            </p>
            <ul className="space-y-2 mb-6">
              <li className="text-gray-700 font-light">First name and last name</li>
              <li className="text-gray-700 font-light">Email address</li>
              <li className="text-gray-700 font-light">Phone number (optional)</li>
              <li className="text-gray-700 font-light">Profile information You choose to provide</li>
            </ul>

            <h4 className="text-lg font-medium text-gray-900 mb-3">Usage Data</h4>
            <p className="text-gray-700 font-light leading-relaxed mb-4">
              We automatically collect Usage Data when You use the Service, including:
            </p>
            <ul className="space-y-2 mb-6">
              <li className="text-gray-700 font-light">Device type, operating system, and app version</li>
              <li className="text-gray-700 font-light">IP address</li>
              <li className="text-gray-700 font-light">Browser type and version</li>
              <li className="text-gray-700 font-light">Pages visited and time spent</li>
              <li className="text-gray-700 font-light">Time stamps and activity logs</li>
              <li className="text-gray-700 font-light">Crash data and performance metrics</li>
              <li className="text-gray-700 font-light">Unique device identifiers</li>
            </ul>
            <p className="text-gray-700 font-light leading-relaxed mb-6">
              This data helps Us secure, improve, and optimize the Service.
            </p>

            <h4 className="text-lg font-medium text-gray-900 mb-3">User Content</h4>
            <p className="text-gray-700 font-light leading-relaxed mb-4">
              User Content includes any data, files, or information You upload or submit through the Service:
            </p>
            <ul className="space-y-2 mb-6">
              <li className="text-gray-700 font-light">Photos and images</li>
              <li className="text-gray-700 font-light">Text entries and notes</li>
              <li className="text-gray-700 font-light">Profile information</li>
              <li className="text-gray-700 font-light">Any other content You choose to share</li>
            </ul>
            <p className="text-gray-700 font-light leading-relaxed mb-6">
              We use this information to provide features of Our Service, to improve and customize Our Service. The information may be uploaded to the Company's servers and/or a Service Provider's server or it may be simply stored on Your device.
            </p>

            <h4 className="text-lg font-medium text-gray-900 mb-3">Cookies and Tracking Technologies</h4>
            <p className="text-gray-700 font-light leading-relaxed mb-6">
              The Website uses cookies, tags, and similar technologies for analytics, functionality, and security. The Application may use mobile identifiers for analytics and to improve Your experience.
            </p>
          </section>

          {/* Use of Your Personal Data */}
          <section className="mb-12">
            <h3 className="text-xl font-normal text-gray-900 mb-4">Use of Your Personal Data</h3>
            <p className="text-gray-700 font-light leading-relaxed mb-4">
              The Company may use Personal Data for the following purposes:
            </p>
            <ul className="space-y-3 mb-6">
              <li className="text-gray-700 font-light">
                <strong className="font-medium text-gray-900">To provide and maintain our Service:</strong> Including account management, feature delivery, and monitoring usage.
              </li>
              <li className="text-gray-700 font-light">
                <strong className="font-medium text-gray-900">To improve the Service:</strong> Analyzing usage patterns, debugging issues, and enhancing performance.
              </li>
              <li className="text-gray-700 font-light">
                <strong className="font-medium text-gray-900">Personalization:</strong> Customizing Your experience and providing relevant recommendations.
              </li>
              <li className="text-gray-700 font-light">
                <strong className="font-medium text-gray-900">Communications:</strong> Sending service updates, notifications, and customer support responses.
              </li>
              <li className="text-gray-700 font-light">
                <strong className="font-medium text-gray-900">Marketing (Optional):</strong> Sending promotional emails unless You opt out.
              </li>
              <li className="text-gray-700 font-light">
                <strong className="font-medium text-gray-900">Legal and Compliance:</strong> Detecting fraud, complying with legal obligations, and protecting Our rights.
              </li>
              <li className="text-gray-700 font-light">
                <strong className="font-medium text-gray-900">Business Transfers:</strong> In connection with mergers, acquisitions, or asset sales, Your information may be transferred as permitted by law.
              </li>
            </ul>
          </section>

          {/* User Content and Data Rights */}
          <section className="mb-12">
            <h2 className="text-3xl font-light text-gray-900 mb-6">
              How We Share Personal Data
            </h2>
            
            <h3 className="text-xl font-normal text-gray-900 mb-4">Service Providers</h3>
            <p className="text-gray-700 font-light leading-relaxed mb-6">
              We may share Your information with third-party service providers who perform services on Our behalf, such as hosting, analytics, payment processing, and customer support.
            </p>

            <h3 className="text-xl font-normal text-gray-900 mb-4">Business Transfers</h3>
            <p className="text-gray-700 font-light leading-relaxed mb-6">
              If We engage in a merger, acquisition, financing, or asset sale, Your information may be transferred as part of that transaction.
            </p>

            <h3 className="text-xl font-normal text-gray-900 mb-4">Legal Compliance</h3>
            <p className="text-gray-700 font-light leading-relaxed mb-6">
              We may disclose Your information if required by law or in response to valid legal requests.
            </p>

            <h3 className="text-xl font-normal text-gray-900 mb-4">With Your Consent</h3>
            <p className="text-gray-700 font-light leading-relaxed mb-6">
              We may share Your information with third parties when You explicitly consent to such sharing.
            </p>

            <h3 className="text-xl font-normal text-gray-900 mb-4">Aggregated or Anonymized Data</h3>
            <p className="text-gray-700 font-light leading-relaxed mb-6">
              We may share aggregated or anonymized data that cannot identify You for research, analytics, or marketing purposes.
            </p>
          </section>

          <section className="mb-12">
            <h2 className="text-3xl font-light text-gray-900 mb-6">
              User Content and Data Rights
            </h2>
            <p className="text-gray-700 font-light leading-relaxed mb-6">
              You retain all rights to the content you create and share through our Service. We do not claim ownership of your personal data or content. You have the right to access, update, or delete your personal information at any time through your account settings.
            </p>
          </section>

          <section className="mb-12">
            <h2 className="text-3xl font-light text-gray-900 mb-6">
              Your Privacy Rights
            </h2>
            <p className="text-gray-700 font-light leading-relaxed mb-4">
              Depending on Your location, You may have the following rights:
            </p>
            <ul className="space-y-2 mb-6">
              <li className="text-gray-700 font-light">Access and receive a copy of Your Personal Data</li>
              <li className="text-gray-700 font-light">Correct inaccurate or incomplete Personal Data</li>
              <li className="text-gray-700 font-light">Delete Your Personal Data</li>
              <li className="text-gray-700 font-light">Object to or restrict processing of Your Personal Data</li>
              <li className="text-gray-700 font-light">Data portability</li>
              <li className="text-gray-700 font-light">Withdraw consent at any time</li>
            </ul>
            <p className="text-gray-700 font-light leading-relaxed mb-6">
              To exercise these rights, please contact us using the information provided below.
            </p>
          </section>

          {/* Retention of Your Personal Data */}
          <section className="mb-12">
            <h3 className="text-xl font-normal text-gray-900 mb-4">Retention of Your Personal Data</h3>
            <p className="text-gray-700 font-light leading-relaxed mb-6">
              The Company will retain Your Personal Data only for as long as is necessary for the purposes set out in this Privacy Policy. We will retain and use Your Personal Data to the extent necessary to comply with our legal obligations, resolve disputes, and enforce our legal agreements and policies.
            </p>
          </section>

          {/* Security of Your Personal Data */}
          <section className="mb-12">
            <h3 className="text-xl font-normal text-gray-900 mb-4">Security of Your Personal Data</h3>
            <p className="text-gray-700 font-light leading-relaxed mb-6">
              The security of Your Personal Data is important to Us, but remember that no method of transmission over the Internet, or method of electronic storage is 100% secure. While We strive to use commercially acceptable means to protect Your Personal Data, We cannot guarantee its absolute security.
            </p>
          </section>

          {/* Children's Privacy */}
          <section className="mb-12">
            <h2 className="text-3xl font-light text-gray-900 mb-6">
              Children's Privacy
            </h2>
            <p className="text-gray-700 font-light leading-relaxed mb-6">
              Our Service does not address anyone under the age of 13. We do not knowingly collect personally identifiable information from anyone under the age of 13. If You are a parent or guardian and You are aware that Your child has provided Us with Personal Data, please contact Us.
            </p>
          </section>

          {/* Changes to this Privacy Policy */}
          <section className="mb-12">
            <h2 className="text-3xl font-light text-gray-900 mb-6">
              Changes to this Privacy Policy
            </h2>
            <p className="text-gray-700 font-light leading-relaxed mb-6">
              We may update Our Privacy Policy from time to time. We will notify You of any changes by posting the new Privacy Policy on this page and updating the "Last updated" date at the top of this Privacy Policy.
            </p>
          </section>

          {/* Contact Us */}
          <section className="mb-12">
            <h2 className="text-3xl font-light text-gray-900 mb-6">
              Contact Us
            </h2>
            <p className="text-gray-700 font-light leading-relaxed mb-4">
              If you have any questions about this Privacy Policy, You can contact us:
            </p>
            <ul className="space-y-2">
              <li className="text-gray-700 font-light">
                By email: <a href="mailto:privacy@basicframework.com" className="text-black hover:underline">privacy@basicframework.com</a>
              </li>
            </ul>
          </section>
        </div>
      </main>

      <Footer />
    </div>
  );
}
