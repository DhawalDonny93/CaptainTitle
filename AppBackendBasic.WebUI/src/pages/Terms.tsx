import Header from '../components/Header';
import Footer from '../components/Footer';

export default function Terms() {

  return (
    <div className="min-h-screen bg-white">
      <Header />

      {/* Main Content */}
      <main className="max-w-4xl mx-auto px-6 py-16">
        {/* Title */}
        <div className="mb-16">
          <h1 className="text-5xl font-light tracking-tight text-gray-900 mb-4">
            Terms of Service
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
              Please read these terms and conditions carefully before using Our Service. Your access to and use of the Service is conditioned upon Your acceptance of and compliance with these Terms and the Captain Title Privacy Policy. By accessing or using the Service, You acknowledge that You have read, understood, and agreed to be bound by these Terms.
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
              For the purposes of these Terms and Conditions:
            </p>
            <ul className="space-y-3 mb-6">
              <li className="text-gray-700 font-light">
                <strong className="font-medium text-gray-900">Affiliate</strong> means any entity that controls, is controlled by, or is under common control with the Company.
              </li>
              <li className="text-gray-700 font-light">
                <strong className="font-medium text-gray-900">Application</strong> means the software program provided by the Company, named Captain Title, including all related features, tools, content, and updates.
              </li>
              <li className="text-gray-700 font-light">
                <strong className="font-medium text-gray-900">Company</strong> (referred to as either "the Company", "We", "Us" or "Our" in this Agreement) refers to Captain Title.
              </li>
              <li className="text-gray-700 font-light">
                <strong className="font-medium text-gray-900">Device</strong> means any device that can access the Service such as a computer, a cellphone or a digital tablet.
              </li>
              <li className="text-gray-700 font-light">
                <strong className="font-medium text-gray-900">Service</strong> means, collectively, the Application, the Website, associated software, content, and any services offered by the Company.
              </li>
              <li className="text-gray-700 font-light">
                <strong className="font-medium text-gray-900">User Content</strong> means any images, photographs, text, metadata, or other content uploaded, submitted, or transmitted by You through the Service.
              </li>
              <li className="text-gray-700 font-light">
                <strong className="font-medium text-gray-900">Website</strong> means your deployed application and any associated subdomains.
              </li>
              <li className="text-gray-700 font-light">
                <strong className="font-medium text-gray-900">Terms and Conditions</strong> (also referred as "Terms") mean these Terms and Conditions that form the entire agreement between You and the Company regarding the use of the Service.
              </li>
              <li className="text-gray-700 font-light">
                <strong className="font-medium text-gray-900">You</strong> means the individual accessing or using the Service, or the company, or other legal entity on behalf of which such individual is accessing or using the Service, as applicable.
              </li>
            </ul>
          </section>

          {/* Acknowledgment */}
          <section className="mb-12">
            <h2 className="text-3xl font-light text-gray-900 mb-6">
              Agreement to Terms
            </h2>
            <p className="text-gray-700 font-light leading-relaxed mb-6">
              These are the Terms and Conditions governing the use of this Service and the agreement that operates between You and the Company. These Terms and Conditions set out the rights and obligations of all users regarding the use of the Service.
            </p>
            <p className="text-gray-700 font-light leading-relaxed mb-6">
              Your access to and use of the Service is conditioned on Your acceptance of and compliance with these Terms and Conditions. These Terms and Conditions apply to all visitors, users and others who access or use the Service.
            </p>
            <p className="text-gray-700 font-light leading-relaxed mb-6">
              By accessing or using the Service You agree to be bound by these Terms and Conditions. If You disagree with any part of these Terms and Conditions then You may not access the Service.
            </p>
          </section>

          {/* Eligibility */}
          <section className="mb-12">
            <h2 className="text-3xl font-light text-gray-900 mb-6">
              Eligibility and Age Requirements
            </h2>
            <p className="text-gray-700 font-light leading-relaxed mb-6">
              You represent and warrant that You are at least 13 years old. If You are between 13 and 17 years old, You may only use the Service with the consent and supervision of a parent or legal guardian, who agrees to be fully responsible for all activities conducted using the Service.
            </p>
          </section>

          {/* User Accounts */}
          <section className="mb-12">
            <h2 className="text-3xl font-light text-gray-900 mb-6">
              User Accounts
            </h2>
            <p className="text-gray-700 font-light leading-relaxed mb-6">
              When You create an account with Us, You must provide Us information that is accurate, complete, and current at all times. Failure to do so constitutes a breach of the Terms, which may result in immediate termination of Your account on Our Service.
            </p>
            <p className="text-gray-700 font-light leading-relaxed mb-6">
              You are responsible for safeguarding the password that You use to access the Service and for any activities or actions under Your password, whether Your password is with Our Service or a Third-Party Social Media Service.
            </p>
            <p className="text-gray-700 font-light leading-relaxed mb-6">
              You agree not to disclose Your password to any third party. You must notify Us immediately upon becoming aware of any breach of security or unauthorized use of Your account.
            </p>
          </section>

          {/* User Content */}
          <section className="mb-12">
            <h2 className="text-3xl font-light text-gray-900 mb-6">
              User Content and License Grant
            </h2>
            <p className="text-gray-700 font-light leading-relaxed mb-6">
              By submitting User Content, You represent that You own or have rights to it. You grant the Company a worldwide, irrevocable, royalty-free, transferable, sublicensable license to use, reproduce, analyze, process, modify, publish, and create derivative works from the User Content to operate and improve the Service.
            </p>
            <p className="text-gray-700 font-light leading-relaxed mb-6">
              You agree not to upload unlawful, harmful, offensive, or infringing content.
            </p>
          </section>

          {/* Prohibited Uses */}
          <section className="mb-12">
            <h2 className="text-3xl font-light text-gray-900 mb-6">
              Prohibited Uses
            </h2>
            <p className="text-gray-700 font-light leading-relaxed mb-4">
              You agree not to:
            </p>
            <ul className="space-y-2 mb-6">
              <li className="text-gray-700 font-light">Upload violent, explicit, or unlawful content</li>
              <li className="text-gray-700 font-light">Reverse engineer or perform competitive analysis</li>
              <li className="text-gray-700 font-light">Circumvent security protections or gain unauthorized access</li>
              <li className="text-gray-700 font-light">Upload images of individuals without lawful consent</li>
              <li className="text-gray-700 font-light">Interfere with the Service's operation or other users' access</li>
              <li className="text-gray-700 font-light">Use the Service for any illegal or unauthorized purpose</li>
            </ul>
          </section>

          {/* Subscriptions */}
          <section className="mb-12">
            <h2 className="text-3xl font-light text-gray-900 mb-6">
              Subscription, Billing, and Payments
            </h2>
            
            <h3 className="text-xl font-normal text-gray-900 mb-4">Subscription Period</h3>
            <p className="text-gray-700 font-light leading-relaxed mb-6">
              The Service or some parts of the Service are available only with a paid Subscription. You will be billed in advance on a recurring and periodic basis (such as daily, weekly, monthly or annually), depending on the type of Subscription plan you select when purchasing the Subscription.
            </p>

            <h3 className="text-xl font-normal text-gray-900 mb-4">Payment Processing</h3>
            <p className="text-gray-700 font-light leading-relaxed mb-6">
              Purchases and subscriptions may be processed through third-party platforms. Their terms govern payment processing, renewals, and refunds. Subscriptions automatically renew unless cancelled at least 24 hours before the current period ends.
            </p>

            <h3 className="text-xl font-normal text-gray-900 mb-4">Free Trial</h3>
            <p className="text-gray-700 font-light leading-relaxed mb-6">
              The Company may, at its sole discretion, offer a Subscription with a Free trial for a limited period of time. You may be required to enter Your billing information in order to sign up for the Free trial.
            </p>

            <h3 className="text-xl font-normal text-gray-900 mb-4">Billing</h3>
            <p className="text-gray-700 font-light leading-relaxed mb-6">
              You shall provide the Company with accurate and complete billing information including full name, address, state, zip code, telephone number, and a valid payment method information.
            </p>

            <h3 className="text-xl font-normal text-gray-900 mb-4">Refunds</h3>
            <p className="text-gray-700 font-light leading-relaxed mb-6">
              Except when required by law, paid Subscription fees are non-refundable. Certain refund requests for Subscriptions may be considered by the Company on a case-by-case basis and granted at the sole discretion of the Company.
            </p>
          </section>

          {/* Third-Party Services */}
          <section className="mb-12">
            <h2 className="text-3xl font-light text-gray-900 mb-6">
              Third-Party Services and Links
            </h2>
            <p className="text-gray-700 font-light leading-relaxed mb-6">
              The Service may include links or integrations with third-party content or services. The Company does not control and is not responsible for such services.
            </p>
            <p className="text-gray-700 font-light leading-relaxed mb-6">
              The Company has no control over, and assumes no responsibility for, the content, privacy policies, or practices of any third party web sites or services. You further acknowledge and agree that the Company shall not be responsible or liable, directly or indirectly, for any damage or loss caused or alleged to be caused by or in connection with the use of or reliance on any such content, goods or services available on or through any such web sites or services.
            </p>
          </section>

          {/* Intellectual Property */}
          <section className="mb-12">
            <h2 className="text-3xl font-light text-gray-900 mb-6">
              Intellectual Property
            </h2>
            <p className="text-gray-700 font-light leading-relaxed mb-6">
              All rights in the Service (excluding User Content) are owned exclusively by the Company or its licensors. You may not copy, modify, distribute, sell, or reverse engineer any part of the Service without express written permission from the Company.
            </p>
          </section>

          {/* Termination */}
          <section className="mb-12">
            <h2 className="text-3xl font-light text-gray-900 mb-6">
              Termination
            </h2>
            <p className="text-gray-700 font-light leading-relaxed mb-6">
              We may terminate or suspend Your Account immediately, without prior notice or liability, for any reason whatsoever, including without limitation if You breach these Terms and Conditions.
            </p>
            <p className="text-gray-700 font-light leading-relaxed mb-6">
              Upon termination, Your right to use the Service will cease immediately. If You wish to terminate Your Account, You may simply discontinue using the Service.
            </p>
          </section>

          {/* Limitation of Liability */}
          <section className="mb-12">
            <h2 className="text-3xl font-light text-gray-900 mb-6">
              Limitation of Liability
            </h2>
            <p className="text-gray-700 font-light leading-relaxed mb-6">
              To the maximum extent allowed by law, the Company's total liability shall not exceed the greater of: (a) the amount You paid in the preceding 12 months, or (b) one hundred dollars (USD $100).
            </p>
            <p className="text-gray-700 font-light leading-relaxed mb-6">
              The Company is not liable for indirect, incidental, or consequential damages, including loss of profits or data.
            </p>
            <p className="text-gray-700 font-light leading-relaxed mb-6">
              To the maximum extent permitted by applicable law, in no event shall the Company or its suppliers be liable for any special, incidental, indirect, or consequential damages whatsoever (including, but not limited to, damages for loss of profits, loss of data or other information, for business interruption, for personal injury, loss of privacy arising out of or in any way related to the use of or inability to use the Service, third-party software and/or third-party hardware used with the Service, or otherwise in connection with any provision of this Terms), even if the Company or any supplier has been advised of the possibility of such damages and even if the remedy fails of its essential purpose.
            </p>
          </section>

          {/* "AS IS" and "AS AVAILABLE" Disclaimer */}
          <section className="mb-12">
            <h2 className="text-3xl font-light text-gray-900 mb-6">
              "AS IS" and "AS AVAILABLE" Disclaimer
            </h2>
            <p className="text-gray-700 font-light leading-relaxed mb-6">
              The Service is provided to You "AS IS" and "AS AVAILABLE" and with all faults and defects without warranty of any kind. To the maximum extent permitted under applicable law, the Company, on its own behalf and on behalf of its Affiliates and its and their respective licensors and service providers, expressly disclaims all warranties, whether express, implied, statutory or otherwise, with respect to the Service.
            </p>
          </section>

          {/* Governing Law */}
          <section className="mb-12">
            <h2 className="text-3xl font-light text-gray-900 mb-6">
              Governing Law
            </h2>
            <p className="text-gray-700 font-light leading-relaxed mb-6">
              These Terms are governed by the laws of the applicable jurisdiction, excluding its conflicts of law rules. Your use of the Application may also be subject to other local, state, national, or international laws.
            </p>
          </section>

          {/* Disputes Resolution */}
          <section className="mb-12">
            <h2 className="text-3xl font-light text-gray-900 mb-6">
              Disputes Resolution
            </h2>
            <p className="text-gray-700 font-light leading-relaxed mb-6">
              If You have any concern or dispute about the Service, You agree to first try to resolve the dispute informally by contacting the Company.
            </p>
          </section>

          {/* Severability and Waiver */}
          <section className="mb-12">
            <h2 className="text-3xl font-light text-gray-900 mb-6">
              Severability and Waiver
            </h2>
            
            <h3 className="text-xl font-normal text-gray-900 mb-4">Severability</h3>
            <p className="text-gray-700 font-light leading-relaxed mb-6">
              If any provision of these Terms is held to be unenforceable or invalid, such provision will be changed and interpreted to accomplish the objectives of such provision to the greatest extent possible under applicable law and the remaining provisions will continue in full force and effect.
            </p>

            <h3 className="text-xl font-normal text-gray-900 mb-4">Waiver</h3>
            <p className="text-gray-700 font-light leading-relaxed mb-6">
              Except as provided herein, the failure to exercise a right or to require performance of an obligation under these Terms shall not effect a party's ability to exercise such right or require such performance at any time thereafter nor shall the waiver of a breach constitute a waiver of any subsequent breach.
            </p>
          </section>

          {/* Changes to These Terms and Conditions */}
          <section className="mb-12">
            <h2 className="text-3xl font-light text-gray-900 mb-6">
              Changes to These Terms and Conditions
            </h2>
            <p className="text-gray-700 font-light leading-relaxed mb-6">
              We reserve the right, at Our sole discretion, to modify or replace these Terms at any time. If a revision is material We will make reasonable efforts to provide at least 30 days' notice prior to any new terms taking effect. What constitutes a material change will be determined at Our sole discretion.
            </p>
            <p className="text-gray-700 font-light leading-relaxed mb-6">
              By continuing to access or use Our Service after those revisions become effective, You agree to be bound by the revised terms. If You do not agree to the new terms, in whole or in part, please stop using the website and the Service.
            </p>
          </section>

          {/* Contact Us */}
          <section className="mb-12">
            <h2 className="text-3xl font-light text-gray-900 mb-6">
              Contact Us
            </h2>
            <p className="text-gray-700 font-light leading-relaxed mb-4">
              If you have any questions about these Terms and Conditions, You can contact us:
            </p>
            <ul className="space-y-2">
              <li className="text-gray-700 font-light">
                By email: <a href="mailto:legal@captaintitle.com" className="text-black hover:underline">legal@captaintitle.com</a>
              </li>
            </ul>
          </section>
        </div>
      </main>

      <Footer />
    </div>
  );
}
