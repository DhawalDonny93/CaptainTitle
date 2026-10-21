import { api } from '../lib/api';

export interface PricingPlan {
  id: string;
  name: string;
  description: string;
  price: number;
  currency: string;
  interval: string;
  stripePriceId: string;
  features: string[];
  isPopular: boolean;
}

export interface CreateCheckoutSessionRequest {
  priceId: string;
  projectId: number;
  successUrl: string;
  cancelUrl: string;
}

export interface CreateCheckoutSessionResponse {
  sessionId: string;
  checkoutUrl: string;
}

export interface CustomerPortalRequest {
  returnUrl: string;
}

export interface CustomerPortalResponse {
  portalUrl: string;
}

const paymentService = {
  // Get available pricing plans
  async getPricingPlans(): Promise<PricingPlan[]> {
    const response = await api.get('/payments/pricing-plans');
    return response.data;
  },

  // Create Stripe checkout session
  async createCheckoutSession(request: CreateCheckoutSessionRequest): Promise<CreateCheckoutSessionResponse> {
    const response = await api.post('/payments/create-checkout-session', request);
    return response.data;
  },

  // Create customer portal session
  async createCustomerPortal(request: CustomerPortalRequest): Promise<CustomerPortalResponse> {
    const response = await api.post('/payments/customer-portal', request);
    return response.data;
  },

  // Redirect to Stripe checkout
  async redirectToCheckout(priceId: string, projectId: number = 1): Promise<void> {
    const baseUrl = window.location.origin;
    const request: CreateCheckoutSessionRequest = {
      priceId,
      projectId,
      successUrl: `${baseUrl}/payment-success?session_id={CHECKOUT_SESSION_ID}`,
      cancelUrl: `${baseUrl}/pricing`,
    };

    const response = await paymentService.createCheckoutSession(request);
    window.location.href = response.checkoutUrl;
  },

  // Redirect to customer portal
  async redirectToCustomerPortal(): Promise<void> {
    const baseUrl = window.location.origin;
    const request: CustomerPortalRequest = {
      returnUrl: `${baseUrl}/settings`,
    };

    const response = await paymentService.createCustomerPortal(request);
    window.location.href = response.portalUrl;
  },
};

export default paymentService;
