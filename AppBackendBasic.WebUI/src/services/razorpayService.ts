import { api } from '../lib/api';

export interface RazorpayCheckoutOptions {
  key: string;
  amount: number;
  currency: string;
  name: string;
  description: string;
  order_id: string;
  prefill: {
    name: string;
    email: string;
    contact: string;
  };
  theme: {
    color: string;
  };
  handler: (response: RazorpayPaymentResponse) => void;
  modal: {
    ondismiss: () => void;
  };
}

export interface RazorpayPaymentResponse {
  razorpay_payment_id: string;
  razorpay_order_id: string;
  razorpay_signature: string;
}

export interface CreateCheckoutSessionResponse {
  sessionId: string;
  checkoutUrl: string;
}

export interface PricingPlan {
  id: string;
  name: string;
  description: string;
  price: number;
  currency: string;
  billingCycle: string;
  features: string[];
  razorpayPlanId: string | null;
  isPopular: boolean;
}

const razorpayService = {
  // Load Razorpay script
  loadRazorpayScript(): Promise<boolean> {
    return new Promise((resolve) => {
      if ((window as any).Razorpay) {
        resolve(true);
        return;
      }

      const script = document.createElement('script');
      script.src = 'https://checkout.razorpay.com/v1/checkout.js';
      script.onload = () => resolve(true);
      script.onerror = () => resolve(false);
      document.body.appendChild(script);
    });
  },

  // Get pricing plans
  async getPricingPlans(): Promise<PricingPlan[]> {
    const response = await api.get('/payments/pricing-plans');
    return response.data;
  },

  // Create checkout session
  async createCheckoutSession(priceId: string, projectId: number): Promise<CreateCheckoutSessionResponse> {
    const response = await api.post('/payments/create-checkout-session', {
      priceId,
      projectId,
      successUrl: `${window.location.origin}/payment/success`,
      cancelUrl: `${window.location.origin}/payment/cancelled`,
    });
    return response.data;
  },

  // Open Razorpay checkout
  async openCheckout(options: Partial<RazorpayCheckoutOptions>): Promise<void> {
    const scriptLoaded = await this.loadRazorpayScript();
    if (!scriptLoaded) {
      throw new Error('Failed to load Razorpay SDK');
    }

    const razorpay = new (window as any).Razorpay(options);
    razorpay.open();
  },

  // Create subscription checkout
  async createSubscriptionCheckout(
    priceId: string,
    projectId: number,
    userInfo: { name: string; email: string; contact: string }
  ): Promise<void> {
    try {
      // Create checkout session
      const session = await this.createCheckoutSession(priceId, projectId);

      // Get Razorpay key from backend or config
      const razorpayKey = import.meta.env.VITE_RAZORPAY_KEY_ID || 'rzp_test_XXXXXXXX';

      // Open Razorpay checkout
      await this.openCheckout({
        key: razorpayKey,
        name: 'AISEO Platform',
        description: 'Premium Subscription',
        order_id: session.sessionId,
        prefill: {
          name: userInfo.name,
          email: userInfo.email,
          contact: userInfo.contact,
        },
        theme: {
          color: '#000000',
        },
        handler: async (response: RazorpayPaymentResponse) => {
          // Verify payment on backend
          await this.verifyPayment(response);
          // Redirect to success page
          window.location.href = '/payment/success';
        },
        modal: {
          ondismiss: () => {
            window.location.href = '/payment/cancelled';
          },
        },
      });
    } catch (error) {
      console.error('Error creating subscription checkout:', error);
      throw error;
    }
  },

  // Verify payment
  async verifyPayment(paymentResponse: RazorpayPaymentResponse): Promise<void> {
    await api.post('/payments/verify-payment', paymentResponse);
  },

  // Get customer portal URL
  async getCustomerPortalUrl(): Promise<string> {
    const response = await api.post('/payments/create-customer-portal-session', {
      returnUrl: window.location.href,
    });
    return response.data.portalUrl;
  },

  // Cancel subscription
  async cancelSubscription(subscriptionId: string): Promise<void> {
    await api.post(`/payments/cancel-subscription/${subscriptionId}`);
  },

  // Get subscription details
  async getSubscriptionDetails(): Promise<any> {
    const response = await api.get('/payments/subscription');
    return response.data;
  },
};

export default razorpayService;
