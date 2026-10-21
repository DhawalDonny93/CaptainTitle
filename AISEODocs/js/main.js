// AISEO Documentation JavaScript
// Lightweight, fast, and efficient

(function() {
    'use strict';

    // Initialize on DOM ready
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', init);
    } else {
        init();
    }

    function init() {
        initSearch();
        initNavigation();
        initTableOfContents();
        initCodeCopy();
        initSmoothScroll();
        initMobileMenu();
    }

    // Search Functionality
    function initSearch() {
        const searchInput = document.getElementById('search-input');
        if (!searchInput) return;

        let searchTimeout;
        searchInput.addEventListener('input', function(e) {
            clearTimeout(searchTimeout);
            searchTimeout = setTimeout(() => {
                performSearch(e.target.value);
            }, 300);
        });
    }

    function performSearch(query) {
        if (!query || query.length < 2) return;
        
        // Simple client-side search
        const content = document.querySelector('.doc-content');
        if (!content) return;

        const text = content.textContent.toLowerCase();
        const searchQuery = query.toLowerCase();
        
        if (text.includes(searchQuery)) {
            highlightText(content, searchQuery);
        }
    }

    function highlightText(element, query) {
        // Remove previous highlights
        const highlighted = element.querySelectorAll('.highlight');
        highlighted.forEach(el => {
            const parent = el.parentNode;
            parent.replaceChild(document.createTextNode(el.textContent), el);
            parent.normalize();
        });

        if (!query) return;

        // Add new highlights
        const walker = document.createTreeWalker(
            element,
            NodeFilter.SHOW_TEXT,
            null,
            false
        );

        const nodesToReplace = [];
        while (walker.nextNode()) {
            const node = walker.currentNode;
            if (node.nodeValue.toLowerCase().includes(query)) {
                nodesToReplace.push(node);
            }
        }

        nodesToReplace.forEach(node => {
            const span = document.createElement('span');
            span.className = 'highlight';
            span.style.backgroundColor = '#fef3c7';
            span.style.padding = '0 2px';
            span.textContent = node.nodeValue;
            node.parentNode.replaceChild(span, node);
        });
    }

    // Navigation Active State
    function initNavigation() {
        const navItems = document.querySelectorAll('.nav-item');
        const currentPath = window.location.pathname;

        navItems.forEach(item => {
            if (item.getAttribute('href') === currentPath || 
                item.getAttribute('href') === currentPath.split('/').pop()) {
                item.classList.add('active');
            }

            item.addEventListener('click', function() {
                navItems.forEach(nav => nav.classList.remove('active'));
                this.classList.add('active');
            });
        });
    }

    // Table of Contents
    function initTableOfContents() {
        const content = document.querySelector('.doc-content');
        const tocNav = document.querySelector('.toc-nav');
        
        if (!content || !tocNav) return;

        const headings = content.querySelectorAll('h2, h3');
        const tocLinks = tocNav.querySelectorAll('.toc-link');

        // Update active TOC link on scroll
        let ticking = false;
        window.addEventListener('scroll', () => {
            if (!ticking) {
                window.requestAnimationFrame(() => {
                    updateActiveTocLink(headings, tocLinks);
                    ticking = false;
                });
                ticking = true;
            }
        });
    }

    function updateActiveTocLink(headings, tocLinks) {
        const scrollPos = window.scrollY + 100;

        headings.forEach((heading, index) => {
            const top = heading.offsetTop;
            const bottom = top + heading.offsetHeight;

            if (scrollPos >= top && scrollPos < bottom) {
                tocLinks.forEach(link => link.classList.remove('active'));
                if (tocLinks[index]) {
                    tocLinks[index].classList.add('active');
                }
            }
        });
    }

    // Code Copy Buttons
    function initCodeCopy() {
        const codeBlocks = document.querySelectorAll('pre code');
        
        codeBlocks.forEach(block => {
            const pre = block.parentElement;
            const button = document.createElement('button');
            button.className = 'copy-button';
            button.textContent = 'Copy';
            button.style.cssText = `
                position: absolute;
                top: 0.5rem;
                right: 0.5rem;
                padding: 0.25rem 0.75rem;
                background: rgba(255, 255, 255, 0.1);
                color: white;
                border: 1px solid rgba(255, 255, 255, 0.2);
                border-radius: 0.375rem;
                font-size: 0.75rem;
                cursor: pointer;
                transition: all 0.2s;
            `;

            pre.style.position = 'relative';
            pre.appendChild(button);

            button.addEventListener('click', () => {
                const code = block.textContent;
                navigator.clipboard.writeText(code).then(() => {
                    button.textContent = 'Copied!';
                    setTimeout(() => {
                        button.textContent = 'Copy';
                    }, 2000);
                });
            });

            button.addEventListener('mouseenter', () => {
                button.style.background = 'rgba(255, 255, 255, 0.2)';
            });

            button.addEventListener('mouseleave', () => {
                button.style.background = 'rgba(255, 255, 255, 0.1)';
            });
        });
    }

    // Smooth Scroll
    function initSmoothScroll() {
        document.querySelectorAll('a[href^="#"]').forEach(anchor => {
            anchor.addEventListener('click', function(e) {
                const href = this.getAttribute('href');
                if (href === '#') return;

                e.preventDefault();
                const target = document.querySelector(href);
                
                if (target) {
                    const offset = 80;
                    const targetPosition = target.offsetTop - offset;
                    
                    window.scrollTo({
                        top: targetPosition,
                        behavior: 'smooth'
                    });

                    // Update URL without jumping
                    history.pushState(null, null, href);
                }
            });
        });
    }

    // Mobile Menu Toggle
    function initMobileMenu() {
        const sidebar = document.querySelector('.sidebar');
        if (!sidebar) return;

        // Create mobile menu button
        const menuButton = document.createElement('button');
        menuButton.className = 'mobile-menu-button';
        menuButton.innerHTML = '☰';
        menuButton.style.cssText = `
            display: none;
            position: fixed;
            bottom: 1rem;
            right: 1rem;
            width: 3rem;
            height: 3rem;
            background: var(--primary-color);
            color: white;
            border: none;
            border-radius: 50%;
            font-size: 1.5rem;
            cursor: pointer;
            box-shadow: 0 4px 6px rgba(0, 0, 0, 0.1);
            z-index: 1000;
        `;

        document.body.appendChild(menuButton);

        // Show button on mobile
        if (window.innerWidth <= 1024) {
            menuButton.style.display = 'block';
        }

        window.addEventListener('resize', () => {
            if (window.innerWidth <= 1024) {
                menuButton.style.display = 'block';
            } else {
                menuButton.style.display = 'none';
                sidebar.classList.remove('open');
            }
        });

        menuButton.addEventListener('click', () => {
            sidebar.classList.toggle('open');
            menuButton.innerHTML = sidebar.classList.contains('open') ? '✕' : '☰';
        });

        // Close sidebar when clicking outside
        document.addEventListener('click', (e) => {
            if (!sidebar.contains(e.target) && e.target !== menuButton) {
                sidebar.classList.remove('open');
                menuButton.innerHTML = '☰';
            }
        });
    }

    // Keyboard shortcuts
    document.addEventListener('keydown', (e) => {
        // Ctrl/Cmd + K for search
        if ((e.ctrlKey || e.metaKey) && e.key === 'k') {
            e.preventDefault();
            const searchInput = document.getElementById('search-input');
            if (searchInput) {
                searchInput.focus();
            }
        }

        // Escape to close mobile menu
        if (e.key === 'Escape') {
            const sidebar = document.querySelector('.sidebar');
            if (sidebar && sidebar.classList.contains('open')) {
                sidebar.classList.remove('open');
                const menuButton = document.querySelector('.mobile-menu-button');
                if (menuButton) {
                    menuButton.innerHTML = '☰';
                }
            }
        }
    });

    // Add loading indicator for page transitions
    window.addEventListener('beforeunload', () => {
        document.body.style.opacity = '0.5';
    });

    // Performance optimization: Lazy load images
    if ('IntersectionObserver' in window) {
        const imageObserver = new IntersectionObserver((entries) => {
            entries.forEach(entry => {
                if (entry.isIntersecting) {
                    const img = entry.target;
                    img.src = img.dataset.src;
                    img.classList.remove('lazy');
                    imageObserver.unobserve(img);
                }
            });
        });

        document.querySelectorAll('img.lazy').forEach(img => {
            imageObserver.observe(img);
        });
    }

    // Add external link indicators
    document.querySelectorAll('a[href^="http"]').forEach(link => {
        if (!link.hostname.includes(window.location.hostname)) {
            link.setAttribute('target', '_blank');
            link.setAttribute('rel', 'noopener noreferrer');
            link.innerHTML += ' ↗';
        }
    });

    console.log('AISEO Documentation loaded successfully! 📚');
})();
