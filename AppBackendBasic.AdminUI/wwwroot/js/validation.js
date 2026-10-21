// Client-side form validation utilities for AdminUI

(function() {
    'use strict';

    // Initialize validation on page load
    document.addEventListener('DOMContentLoaded', function() {
        initializeFormValidation();
    });

    function initializeFormValidation() {
        const forms = document.querySelectorAll('.needs-validation');
        
        Array.from(forms).forEach(function(form) {
            form.addEventListener('submit', function(event) {
                if (!form.checkValidity()) {
                    event.preventDefault();
                    event.stopPropagation();
                }
                form.classList.add('was-validated');
            }, false);
        });

        // Real-time validation for specific fields
        addRealTimeValidation();
    }

    function addRealTimeValidation() {
        // Email validation
        const emailInputs = document.querySelectorAll('input[type="email"]');
        emailInputs.forEach(input => {
            input.addEventListener('blur', function() {
                validateEmail(this);
            });
        });

        // URL validation
        const urlInputs = document.querySelectorAll('input[data-validate="url"]');
        urlInputs.forEach(input => {
            input.addEventListener('blur', function() {
                validateUrl(this);
            });
        });

        // Phone validation
        const phoneInputs = document.querySelectorAll('input[type="tel"]');
        phoneInputs.forEach(input => {
            input.addEventListener('blur', function() {
                validatePhone(this);
            });
        });

        // Password strength
        const passwordInputs = document.querySelectorAll('input[type="password"][data-validate="strength"]');
        passwordInputs.forEach(input => {
            input.addEventListener('input', function() {
                validatePasswordStrength(this);
            });
        });

        // Confirm password
        const confirmPasswordInputs = document.querySelectorAll('input[data-confirm-password]');
        confirmPasswordInputs.forEach(input => {
            input.addEventListener('input', function() {
                const passwordId = this.getAttribute('data-confirm-password');
                const passwordInput = document.getElementById(passwordId);
                if (passwordInput) {
                    validatePasswordMatch(passwordInput, this);
                }
            });
        });
    }

    function validateEmail(input) {
        const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
        const isValid = emailRegex.test(input.value);
        
        setValidationState(input, isValid, 'Please enter a valid email address');
        return isValid;
    }

    function validateUrl(input) {
        if (!input.value) return true; // Optional field
        
        try {
            new URL(input.value);
            setValidationState(input, true, '');
            return true;
        } catch {
            setValidationState(input, false, 'Please enter a valid URL (e.g., https://example.com)');
            return false;
        }
    }

    function validatePhone(input) {
        if (!input.value) return true; // Optional field
        
        const phoneRegex = /^[\d\s\-\+\(\)]+$/;
        const isValid = phoneRegex.test(input.value) && input.value.replace(/\D/g, '').length >= 10;
        
        setValidationState(input, isValid, 'Please enter a valid phone number');
        return isValid;
    }

    function validatePasswordStrength(input) {
        const password = input.value;
        const minLength = 8;
        
        let strength = 0;
        let feedback = [];

        if (password.length >= minLength) strength++;
        else feedback.push(`At least ${minLength} characters`);

        if (/[a-z]/.test(password)) strength++;
        else feedback.push('One lowercase letter');

        if (/[A-Z]/.test(password)) strength++;
        else feedback.push('One uppercase letter');

        if (/\d/.test(password)) strength++;
        else feedback.push('One number');

        if (/[^a-zA-Z0-9]/.test(password)) strength++;
        else feedback.push('One special character');

        // Update strength indicator
        const strengthIndicator = input.parentElement.querySelector('.password-strength');
        if (strengthIndicator) {
            strengthIndicator.className = 'password-strength';
            if (strength <= 2) {
                strengthIndicator.classList.add('weak');
                strengthIndicator.textContent = 'Weak';
            } else if (strength <= 3) {
                strengthIndicator.classList.add('medium');
                strengthIndicator.textContent = 'Medium';
            } else {
                strengthIndicator.classList.add('strong');
                strengthIndicator.textContent = 'Strong';
            }
        }

        const isValid = strength >= 4;
        setValidationState(input, isValid, feedback.length > 0 ? 'Password must contain: ' + feedback.join(', ') : '');
        return isValid;
    }

    function validatePasswordMatch(passwordInput, confirmInput) {
        const isValid = passwordInput.value === confirmInput.value;
        setValidationState(confirmInput, isValid, 'Passwords do not match');
        return isValid;
    }

    function setValidationState(input, isValid, message) {
        const feedbackElement = input.parentElement.querySelector('.invalid-feedback') || 
                               input.parentElement.querySelector('.validation-message');
        
        if (isValid) {
            input.classList.remove('is-invalid');
            input.classList.add('is-valid');
            if (feedbackElement) {
                feedbackElement.textContent = '';
                feedbackElement.style.display = 'none';
            }
        } else {
            input.classList.remove('is-valid');
            input.classList.add('is-invalid');
            if (feedbackElement) {
                feedbackElement.textContent = message;
                feedbackElement.style.display = 'block';
            } else {
                // Create feedback element if it doesn't exist
                const feedback = document.createElement('div');
                feedback.className = 'invalid-feedback';
                feedback.textContent = message;
                input.parentElement.appendChild(feedback);
            }
        }
    }

    // Export functions for global use
    window.formValidation = {
        validateEmail: validateEmail,
        validateUrl: validateUrl,
        validatePhone: validatePhone,
        validatePasswordStrength: validatePasswordStrength,
        validatePasswordMatch: validatePasswordMatch,
        setValidationState: setValidationState
    };
})();
